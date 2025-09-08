using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.Client;
using ProtonDrive.Client.FileUploading;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.Logging;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoAlbumImporter
{
    private readonly PhotoImportPipelineParameters _parameters;
    private readonly IPhotoFileSystemClient<long> _localFileSystemClient;
    private readonly IPhotoFileUploader _photoFileUploader;
    private readonly IPhotoAlbumService _photoAlbumService;
    private readonly IPhotoDuplicateService _duplicateService;
    private readonly IPhotoAlbumNameProvider _photoAlbumNameProvider;
    private readonly ImportProgress _progress;
    private readonly ILogger _logger;

    private readonly SemaphoreSlim _semaphore;

    private string? _albumLinkId;

    public PhotoAlbumImporter(
        PhotoImportPipelineParameters parameters,
        IPhotoFileSystemClient<long> localFileSystemClient,
        IPhotoFileUploader photoFileUploader,
        IPhotoAlbumService photoAlbumService,
        IPhotoDuplicateService duplicateService,
        IPhotoAlbumNameProvider photoAlbumNameProvider,
        ImportProgress progress,
        ILogger logger)
    {
        _parameters = parameters;
        _localFileSystemClient = localFileSystemClient;
        _photoFileUploader = photoFileUploader;
        _photoAlbumService = photoAlbumService;
        _photoAlbumNameProvider = photoAlbumNameProvider;
        _duplicateService = duplicateService;
        _progress = progress;
        _logger = logger;

        _semaphore = new SemaphoreSlim(_parameters.MaxNumberOfConcurrentFileTransfers);
    }

    public async Task CreateAlbumAndImportPhotosAsync(NodeInfo<long> albumFolder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_parameters.FolderCurrentPosition.HasValue)
        {
            TrySetResumedAlbumLinkId(albumFolder.Path, _parameters.FolderCurrentPosition.Value);
        }

        var batch = new List<NodeInfo<long>>(_parameters.DuplicationCheckBatchSize);

        await foreach (var file in _localFileSystemClient.EnumeratePhotoFilesAsync(albumFolder, cancellationToken).ConfigureAwait(false))
        {
            batch.Add(file);

            if (batch.Count == _parameters.DuplicationCheckBatchSize)
            {
                await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                batch.Clear();
            }
        }

        await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        batch.Clear();
    }

    private static bool IsExpectedException(Exception exception)
    {
        return exception.IsFileAccessException() || exception.IsDriveClientException() || exception is FileSystemClientException;
    }

    private void TrySetResumedAlbumLinkId(string albumFolderPath, PhotoImportFolderCurrentPosition currentPosition)
    {
        var lastProcessedAlbumPath = Path.Combine(_parameters.FolderPath, currentPosition.RelativePath);

        if (!string.Equals(albumFolderPath, lastProcessedAlbumPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _albumLinkId = currentPosition.AlbumLinkId;

        var folderPathToLog = _logger.GetSensitiveValueForLogging(currentPosition.RelativePath);
        _logger.LogInformation("Photo import resumed: Folder \"{Path}\", Album ID {AlbumLinkId}", folderPathToLog, _albumLinkId);
    }

    private async Task ProcessBatchAsync(List<NodeInfo<long>> files, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (files.Count == 0)
        {
            return;
        }

        try
        {
            var importTasks = new List<Task>(_parameters.MaxNumberOfConcurrentFileTransfers);

            await foreach (var filePath in GetFilePathsExcludingDuplicatesAsync(files, cancellationToken).ConfigureAwait(false))
            {
                if (_albumLinkId is null)
                {
                    await CreateAlbumAsync(filePath, cancellationToken).ConfigureAwait(false);
                }

                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                var importTask = ImportAsync(filePath, cancellationToken);
                importTasks.Add(importTask);
            }

            await Task.WhenAll(importTasks).ConfigureAwait(false);
        }
        catch (AggregateException exception)
        {
            exception.Handle(IsExpectedException);

            throw new PhotoImportException($"Processing batch of {files.Count} photos failed", exception);
        }
        catch (Exception exception) when (IsExpectedException(exception))
        {
            throw new PhotoImportException($"Processing batch of {files.Count} photos failed", exception);
        }
    }

    private async Task CreateAlbumAsync(string filePath, CancellationToken cancellationToken)
    {
        var (albumName, albumRelativePath) = _photoAlbumNameProvider.GetAlbumNameFromPath(
            _parameters.FolderPath.AsSpan(),
            Path.GetDirectoryName(filePath.AsSpan()));

        _albumLinkId = await _photoAlbumService.CreateAlbumAsync(albumName, _parameters.ParentLinkId, cancellationToken).ConfigureAwait(false);

        _progress.RaiseAlbumCreated(new PhotoImportFolderCurrentPosition { AlbumLinkId = _albumLinkId, RelativePath = albumRelativePath });
    }

    private async Task ImportAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uploadedFile = await UploadFileAsync(filePath, cancellationToken).ConfigureAwait(false);

            var albumLinkId = _albumLinkId ?? throw new PhotoImportException("Cannot add file to album: missing album link ID");

            await _photoAlbumService.AddToAlbumAsync(albumLinkId, uploadedFile, cancellationToken).ConfigureAwait(false);

            _progress.RaiseFileImported();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<NodeInfo<string>> UploadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var filePathToLog = _logger.GetSensitiveValueForLogging(filePath);

        try
        {
            _logger.LogInformation("Importing file \"{Path}\"", filePathToLog);

            var importedFile = await _photoFileUploader.UploadFileAsync(filePath, _parameters.ParentLinkId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Imported file \"{Path}\"", filePathToLog);

            _progress.RaiseFileUploaded(filePath);

            return importedFile;
        }
        catch (Exception exception) when (IsExpectedException(exception))
        {
            _logger.LogWarning("Failed to import file \"{Path}\": {Message}", filePathToLog, exception.Message);

            _progress.RaiseFileUploadFailed(filePath, exception);

            throw;
        }
    }

    private async IAsyncEnumerable<string> GetFilePathsExcludingDuplicatesAsync(
        IReadOnlyList<NodeInfo<long>> nodes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var nameCollisions = await _duplicateService.GetNameCollisionsAsync(
            _parameters.VolumeId,
            _parameters.ShareId,
            _parameters.ParentLinkId,
            nodes.Select(x => x.Name),
            cancellationToken).ConfigureAwait(false);

        foreach (var node in nodes)
        {
            var (duplicate, sha1Digest) = await FindPhotoDuplicate(node.Path, node.Name, nameCollisions, cancellationToken).ConfigureAwait(false);

            if (duplicate is not null)
            {
                if (_albumLinkId is not null && sha1Digest is not null)
                {
                    var file = NodeInfo<string>.File()
                        .WithId(duplicate.LinkId)
                        .WithName(node.Name)
                        .WithSha1Digest(sha1Digest);

                    await _photoAlbumService.AddToAlbumAsync(_albumLinkId, file, cancellationToken).ConfigureAwait(false);
                    _progress.RaiseFileImported();
                }

                continue;
            }

            yield return node.Path;
        }
    }

    private async Task<(PhotoNameCollision? Duplicate, string? Sha1Digest)> FindPhotoDuplicate(
        string filePath,
        string fileName,
        ILookup<string, PhotoNameCollision> nameCollisions,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!nameCollisions.Contains(fileName))
            {
                return (Duplicate: null, Sha1Digest: null);
            }

            // A file with the same name can be uploaded multiple times if its content differs.
            // Therefore, when checking for duplicates, we must evaluate all matching filenames.
            foreach (var collision in nameCollisions[fileName])
            {
                var filePathToLog = _logger.GetSensitiveValueForLogging(filePath);

                if (!collision.TryGetContentHashIfNotDraft(out var collisionContentHash, out var draftCreatedByAnotherClient))
                {
                    if (draftCreatedByAnotherClient.Value)
                    {
                        _logger.LogInformation(
                            "Importing photo \"{Path}\" skipped, draft with same name from another client exists, assuming duplicate",
                            filePathToLog);

                        return (collision, Sha1Digest: null);
                    }

                    // The draft will be resumed or overwritten.
                    continue;
                }

                var source = await _localFileSystemClient.OpenFileForReading(NodeInfo<long>.File().WithPath(filePath), cancellationToken)
                    .ConfigureAwait(false);

                await using (source.ConfigureAwait(false))
                {
                    var contentStream = source.GetContentStream();

                    await using (contentStream.ConfigureAwait(false))
                    {
                        var (contentHash, sha1Digest) = await _duplicateService.GetContentHashAndSha1DigestAsync(
                                contentStream,
                                _parameters.ShareId,
                                _parameters.ParentLinkId,
                                cancellationToken)
                            .ConfigureAwait(false);

                        if (contentHash.Equals(collisionContentHash, StringComparison.Ordinal))
                        {
                            _logger.LogInformation("Importing photo \"{Path}\" skipped, duplicate detected", filePathToLog);
                            return (collision, sha1Digest);
                        }
                    }
                }
            }

            return (Duplicate: null, Sha1Digest: null);
        }
        catch (Exception exception) when (IsExpectedException(exception))
        {
            throw new PhotoImportException(
                $"Eligibility cannot be evaluated for file with path \"{_logger.GetSensitiveValueForLogging(filePath)}\"",
                exception);
        }
    }
}
