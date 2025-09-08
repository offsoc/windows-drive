using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Photos.LivePhoto;
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
    private readonly IPhotoAlbumNameProvider _albumNameProvider;
    private readonly ILivePhotoFileDetector _livePhotoFileDetector;
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
        IPhotoAlbumNameProvider albumNameProvider,
        ILivePhotoFileDetector livePhotoFileDetector,
        ImportProgress progress,
        ILogger logger)
    {
        _parameters = parameters;
        _localFileSystemClient = localFileSystemClient;
        _photoFileUploader = photoFileUploader;
        _photoAlbumService = photoAlbumService;
        _albumNameProvider = albumNameProvider;
        _livePhotoFileDetector = livePhotoFileDetector;
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

        var batch = new List<PhotoGroup>(_parameters.DuplicationCheckBatchSize);
        var currentBatchCount = 0;

        var photoFiles = _localFileSystemClient.EnumeratePhotoFilesAsync(albumFolder, cancellationToken);
        var photoGroups = PhotoGroupEnumerator.EnumerateAsync(photoFiles, _livePhotoFileDetector);

        await foreach (var photoGroup in photoGroups.ConfigureAwait(false))
        {
            if (currentBatchCount + photoGroup.Count > _parameters.DuplicationCheckBatchSize)
            {
                await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                batch.Clear();
                currentBatchCount = 0;
            }

            batch.Add(photoGroup);
            currentBatchCount += photoGroup.Count;
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

    private async Task ProcessBatchAsync(IReadOnlyList<PhotoGroup> batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            var importTasks = new List<Task>(_parameters.MaxNumberOfConcurrentFileTransfers);

            await foreach (var photoGroup in GetPhotoGroupsExcludingDuplicatesAsync(batch, cancellationToken).ConfigureAwait(false))
            {
                if (_albumLinkId is null)
                {
                    await CreateAlbumAsync(photoGroup.MainPhoto.Path, cancellationToken).ConfigureAwait(false);
                }

                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                var importTask = ImportAsync(photoGroup, cancellationToken);
                importTasks.Add(importTask);
            }

            await Task.WhenAll(importTasks).ConfigureAwait(false);
        }
        catch (AggregateException exception)
        {
            exception.Handle(IsExpectedException);

            throw new PhotoImportException($"Processing batch of {batch.Count} photos failed", exception);
        }
        catch (Exception exception) when (IsExpectedException(exception))
        {
            throw new PhotoImportException($"Processing batch of {batch.Count} photos failed", exception);
        }
    }

    private async Task CreateAlbumAsync(string filePath, CancellationToken cancellationToken)
    {
        var rootFolderPath = _parameters.FolderPath.AsSpan();
        var currentFolderPath = Path.GetDirectoryName(filePath.AsSpan());

        if (!currentFolderPath.StartsWith(rootFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new PhotoImportException("Album name cannot be created: Root folder path and current folder path are not related");
        }

        var relativePath = (currentFolderPath.Length != rootFolderPath.Length
            ? currentFolderPath[(rootFolderPath.Length + 1)..]
            : ReadOnlySpan<char>.Empty).ToString();

        var albumName = _albumNameProvider.GetAlbumNameFromPath(rootFolderPath, relativePath);

        _albumLinkId = await _photoAlbumService.CreateAlbumAsync(albumName, _parameters.ParentLinkId, cancellationToken).ConfigureAwait(false);

        _progress.RaiseAlbumCreated(new PhotoImportFolderCurrentPosition { AlbumLinkId = _albumLinkId, RelativePath = relativePath });
    }

    private async Task ImportAsync(PhotoGroup photoGroup, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var photosToAddToAlbum = new List<NodeInfo<string>>(1 + photoGroup.RelatedMedia.Count);

            var uploadedFile = await UploadFileAsync(photoGroup.MainPhoto.Path, null, cancellationToken).ConfigureAwait(false);

            var albumLinkId = _albumLinkId ?? throw new PhotoImportException("Cannot add file to album: missing album link ID");

            photosToAddToAlbum.Add(uploadedFile);

            _progress.RaiseFileImported();

            if (photoGroup.RelatedMedia.Count > 0)
            {
                foreach (var relatedMedia in photoGroup.RelatedMedia)
                {
                    var uploadedRelatedFile = await UploadFileAsync(relatedMedia.Path, uploadedFile.Id, cancellationToken).ConfigureAwait(false);

                    photosToAddToAlbum.Add(uploadedRelatedFile);

                    _progress.RaiseFileImported();
                }
            }

            await _photoAlbumService.AddToAlbumAsync(albumLinkId, photosToAddToAlbum, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<NodeInfo<string>> UploadFileAsync(string filePath, string? mainPhotoLinkId, CancellationToken cancellationToken)
    {
        const int maxNumberOfRetries = 2;

        var filePathToLog = _logger.GetSensitiveValueForLogging(filePath);
        var attempt = 1;

        while (true)
        {
            try
            {
                _logger.LogInformation("Importing file \"{Path}\" (attempt {Attempt}/{MaxNumberOfRetries})", filePathToLog, attempt, maxNumberOfRetries);

                var importedFile = await _photoFileUploader.UploadFileAsync(filePath, _parameters.ParentLinkId, mainPhotoLinkId, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Imported file \"{Path}\"", filePathToLog);

                _progress.RaiseFileUploaded(filePath);

                return importedFile;
            }
            catch (PhotoFileSizeMismatchException exception) when (attempt < maxNumberOfRetries)
            {
                ++attempt;

                _progress.RaiseFileUploadFailed(filePath, exception);
            }
            catch (PhotoFileSizeMismatchException exception)
            {
                _progress.RaiseFileUploadFailed(filePath, exception);

                throw;
            }
            catch (Exception exception) when (IsExpectedException(exception))
            {
                _logger.LogWarning("Failed to import file \"{Path}\": {Message}", filePathToLog, exception.Message);

                _progress.RaiseFileUploadFailed(filePath, exception);

                throw;
            }
        }
    }

    private async IAsyncEnumerable<PhotoGroup> GetPhotoGroupsExcludingDuplicatesAsync(
        IReadOnlyList<PhotoGroup> photoGroups,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fileNames = photoGroups.SelectMany(pg => pg.RelatedMedia.Select(x => x.Name).Prepend(pg.MainPhoto.Name));

        var nameCollisions = await _duplicateService.GetNameCollisionsAsync(
            _parameters.VolumeId,
            _parameters.ShareId,
            _parameters.ParentLinkId,
            fileNames,
            cancellationToken).ConfigureAwait(false);

        foreach (var photoGroup in photoGroups)
        {
            var (duplicate, sha1Digest) =
                await FindPhotoDuplicateAsync(photoGroup.MainPhoto.Path, photoGroup.MainPhoto.Name, nameCollisions, cancellationToken).ConfigureAwait(false);

            if (duplicate is not null)
            {
                if (_albumLinkId is not null && sha1Digest is not null)
                {
                    // TODO: This method should not cause side effects, such as uploading files and adding to album
                    await AddDuplicateToAlbumAsync(_albumLinkId, photoGroup, duplicate.LinkId, sha1Digest, nameCollisions, cancellationToken)
                        .ConfigureAwait(false);
                }

                continue;
            }

            yield return photoGroup;
        }
    }

    private async Task AddDuplicateToAlbumAsync(
        string albumLinkId,
        PhotoGroup photo,
        string mainPhotoLinkId,
        string mainPhotoSha1Digest,
        ILookup<string, PhotoNameCollision> nameCollisions,
        CancellationToken cancellationToken)
    {
        var mainPhoto = NodeInfo<string>.File()
            .WithId(mainPhotoLinkId)
            .WithName(photo.MainPhoto.Name)
            .WithSha1Digest(mainPhotoSha1Digest);

        var filesToAddToAlbum = new List<NodeInfo<string>>(1 + photo.RelatedMedia.Count) { mainPhoto };

        foreach (var relatedMedium in photo.RelatedMedia)
        {
            var relatedFile = await GetOrUploadRelatedMediaAsync(relatedMedium, mainPhotoLinkId, nameCollisions, cancellationToken).ConfigureAwait(false);
            filesToAddToAlbum.Add(relatedFile);
        }

        await _photoAlbumService.AddToAlbumAsync(albumLinkId, filesToAddToAlbum, cancellationToken).ConfigureAwait(false);
        _progress.RaiseFilesImported(filesToAddToAlbum.Count);
    }

    private async Task<NodeInfo<string>> GetOrUploadRelatedMediaAsync(
        NodeInfo<string> relatedMedium,
        string mainPhotoLinkId,
        ILookup<string, PhotoNameCollision> nameCollisions,
        CancellationToken cancellationToken)
    {
        var (relatedMediumDuplicate, relatedMediumSha1Digest) = await FindPhotoDuplicateAsync(
            relatedMedium.Path,
            relatedMedium.Name,
            nameCollisions,
            cancellationToken).ConfigureAwait(false);

        if (relatedMediumDuplicate is not null && relatedMediumSha1Digest is not null)
        {
            // Reuse existing duplicate file
            return NodeInfo<string>.File()
                .WithId(relatedMediumDuplicate.LinkId)
                .WithName(relatedMedium.Name)
                .WithSha1Digest(relatedMediumSha1Digest);
        }

        // Upload new file
        var uploadedRelatedFile = await UploadFileAsync(relatedMedium.Path, mainPhotoLinkId, cancellationToken).ConfigureAwait(false);

        if (uploadedRelatedFile.Sha1Digest is null)
        {
            throw new PhotoImportException($"Import failed: missing SHA1 digest for uploaded file with ID {uploadedRelatedFile.Id}");
        }

        return NodeInfo<string>.File()
            .WithId(uploadedRelatedFile.Id)
            .WithName(relatedMedium.Name)
            .WithSha1Digest(uploadedRelatedFile.Sha1Digest);
    }

    private async Task<(PhotoNameCollision? Duplicate, string? Sha1Digest)> FindPhotoDuplicateAsync(
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

                // TODO: Duplicate detection recomputes content hash repeatedly if source file has multiple collisions
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

    private static class PhotoGroupEnumerator
    {
        // TODO: Live Photo pairing relies on file order, especially on extensions; videos first won't pair
        public static async IAsyncEnumerable<PhotoGroup> EnumerateAsync(
            IAsyncEnumerable<NodeInfo<long>> photoFiles,
            ILivePhotoFileDetector livePhotoFileDetector)
        {
            NodeInfo<string>? currentMainFile = null;
            List<NodeInfo<string>> currentRelatedMediaFiles = [];

            await foreach (var nodeInfo in photoFiles.ConfigureAwait(false))
            {
                var file = NodeInfo<string>.File().WithPath(nodeInfo.Path).WithName(nodeInfo.Name);

                if (currentMainFile is null)
                {
                    currentMainFile = file;
                    continue;
                }

                if (livePhotoFileDetector.IsVideoRelatedToLivePhoto(file.Path, currentMainFile.Path))
                {
                    currentRelatedMediaFiles.Add(file);
                    continue;
                }

                yield return new PhotoGroup(currentMainFile, currentRelatedMediaFiles);

                currentMainFile = file;
                currentRelatedMediaFiles = [];
            }

            if (currentMainFile is not null)
            {
                yield return new PhotoGroup(currentMainFile, currentRelatedMediaFiles);
            }
        }
    }

    private record PhotoGroup(NodeInfo<string> MainPhoto, IReadOnlyList<NodeInfo<string>> RelatedMedia)
    {
        public int Count => RelatedMedia.Count + 1;
    }
}
