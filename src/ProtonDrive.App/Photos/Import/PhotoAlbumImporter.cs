using System;
using System.Collections.Generic;
using System.IO;
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
    private readonly IPhotoFileImporter _photoFileImporter;
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
        IPhotoFileImporter photoFileImporter,
        IPhotoAlbumService photoAlbumService,
        IPhotoDuplicateService duplicateService,
        IPhotoAlbumNameProvider photoAlbumNameProvider,
        ImportProgress progress,
        ILogger logger)
    {
        _parameters = parameters;
        _localFileSystemClient = localFileSystemClient;
        _photoFileImporter = photoFileImporter;
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

                var importTask = ImportFileAsync(filePath, cancellationToken);
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

    private async Task ImportFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePathToLog = _logger.GetSensitiveValueForLogging(filePath);

            _logger.LogInformation("Importing photo \"{Path}\"", filePathToLog);

            var importedFile = await _photoFileImporter.ImportFileAsync(filePath, _parameters.ParentLinkId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Imported photo \"{Path}\"", filePathToLog);

            var albumLinkId = _albumLinkId ?? throw new PhotoImportException("Cannot add file to album: missing album link ID");

            await _photoAlbumService.AddToAlbumAsync(albumLinkId, importedFile, cancellationToken).ConfigureAwait(false);

            _progress.RaiseFileImported();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async IAsyncEnumerable<string> GetFilePathsExcludingDuplicatesAsync(
        IReadOnlyList<NodeInfo<long>> nodes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var duplicatesByFileName = await _duplicateService.GetDuplicatesByFilenameAsync(
            _parameters.VolumeId,
            _parameters.ShareId,
            _parameters.ParentLinkId,
            nodes.Select(x => x.Name),
            cancellationToken).ConfigureAwait(false);

        foreach (var node in nodes)
        {
            if (await FileIsAlreadyImported((node.Path, node.Name)).ConfigureAwait(false))
            {
                // TODO: Consider adding the photo file to the album here.
                // This operation is idempotent, so re-adding an already linked photo is safe.
                _progress.RaiseFileImported();
                continue;
            }

            yield return node.Path;
        }

        yield break;

        async Task<bool> FileIsAlreadyImported((string FilePath, string FileName) node)
        {
            try
            {
                if (!duplicatesByFileName.Contains(node.FileName))
                {
                    return false;
                }

                // A file with the same name can be uploaded multiple times if its content differs.
                // Therefore, when checking for duplicates, we must evaluate all matching filenames.
                foreach (var duplicateHashResult in duplicatesByFileName[node.FileName])
                {
                    if (duplicateHashResult.DraftCreatedByAnotherClient)
                    {
                        var filePathToLog = _logger.GetSensitiveValueForLogging(node.FilePath);
                        _logger.LogInformation("Importing photo \"{Path}\" skipped, draft from another client exists", filePathToLog);
                        return true;
                    }

                    if (duplicateHashResult.ContentHash is null)
                    {
                        continue;
                    }

                    var source = await _localFileSystemClient.OpenFileForReading(NodeInfo<long>.File().WithPath(node.FilePath), cancellationToken)
                        .ConfigureAwait(false);

                    await using (source.ConfigureAwait(false))
                    {
                        var contentHash = await _duplicateService.GetContentHash(
                                source.GetContentStream(),
                                _parameters.ShareId,
                                _parameters.ParentLinkId,
                                cancellationToken)
                            .ConfigureAwait(false);

                        if (duplicateHashResult.ContentHash.Equals(contentHash))
                        {
                            var filePathToLog = _logger.GetSensitiveValueForLogging(node.FilePath);
                            _logger.LogInformation("Importing photo \"{Path}\" skipped, duplicate detected", filePathToLog);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception exception) when (IsExpectedException(exception))
            {
                throw new PhotoImportException(
                    $"Eligibility cannot be evaluated for file with path \"{_logger.GetSensitiveValueForLogging(node.FilePath)}\"",
                    exception);
            }
        }
    }
}
