using System;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Photos.LivePhoto;
using ProtonDrive.App.Settings;
using ProtonDrive.Client.FileUploading;
using ProtonDrive.Shared.Configuration;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoImportEngineFactory : IPhotoImportEngineFactory
{
    private readonly Func<FileSystemClientParameters, IFileSystemClient<string>> _remoteFileSystemClientFactory;
    private readonly ILocalFileSystemClientFactory _localFileSystemClientFactory;
    private readonly PhotoFileImporterFactory _photoFileImporterFactory;
    private readonly PhotoAlbumServiceFactory _photoAlbumServiceFactory;
    private readonly IPhotoDuplicateService _duplicateService;
    private readonly IPhotoAlbumNameProvider _photoAlbumNameProvider;
    private readonly ILivePhotoFileDetector _livePhotoFileDetector;
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _maxNumberOfConcurrentFileTransfers;

    public PhotoImportEngineFactory(
        Func<FileSystemClientParameters, IFileSystemClient<string>> remoteFileSystemClientFactory,
        ILocalFileSystemClientFactory localFileSystemClientFactory,
        PhotoFileImporterFactory photoFileImporterFactory,
        PhotoAlbumServiceFactory photoAlbumServiceFactory,
        IPhotoDuplicateService duplicateService,
        IPhotoAlbumNameProvider photoAlbumNameProvider,
        ILivePhotoFileDetector livePhotoFileDetector,
        AppConfig appConfig,
        ILoggerFactory loggerFactory)
    {
        _maxNumberOfConcurrentFileTransfers = appConfig.MaxNumberOfConcurrentFileTransfers;
        _remoteFileSystemClientFactory = remoteFileSystemClientFactory;
        _localFileSystemClientFactory = localFileSystemClientFactory;
        _photoFileImporterFactory = photoFileImporterFactory;
        _photoAlbumServiceFactory = photoAlbumServiceFactory;
        _duplicateService = duplicateService;
        _photoAlbumNameProvider = photoAlbumNameProvider;
        _livePhotoFileDetector = livePhotoFileDetector;
        _loggerFactory = loggerFactory;
    }

    public IPhotoImportEngine CreateEngine(RemoteToLocalMapping mapping, PhotoImportFolderCurrentPosition? currentPosition)
    {
        var volumeId = mapping.Remote.VolumeId ?? throw new ArgumentNullException(nameof(mapping), "Volume ID is required");
        var shareId = mapping.Remote.ShareId ?? throw new ArgumentNullException(nameof(mapping), "Share ID is required");
        var remoteFileSystemClient = _remoteFileSystemClientFactory.Invoke(new FileSystemClientParameters(volumeId, shareId, IsPhotoClient: true));

        var photoAlbumService = _photoAlbumServiceFactory.CreatePhotoAlbumService(remoteFileSystemClient);

        return new PhotoImportEngine(
            mapping,
            currentPosition,
            remoteFileSystemClient,
            _localFileSystemClientFactory,
            _photoFileImporterFactory,
            photoAlbumService,
            _duplicateService,
            _photoAlbumNameProvider,
            _livePhotoFileDetector,
            _maxNumberOfConcurrentFileTransfers,
            _loggerFactory.CreateLogger<PhotoImportEngine>());
    }
}
