using Microsoft.Extensions.Logging;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoAlbumServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public PhotoAlbumServiceFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IPhotoAlbumService CreatePhotoAlbumService(IFileSystemClient<string> remoteFileSystemClient)
    {
        return new PhotoAlbumService(remoteFileSystemClient, _loggerFactory.CreateLogger<PhotoAlbumService>());
    }
}
