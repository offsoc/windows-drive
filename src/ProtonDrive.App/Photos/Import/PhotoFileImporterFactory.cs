using Microsoft.Extensions.Logging;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoFileImporterFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public PhotoFileImporterFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IPhotoFileUploader Create(IPhotoFileSystemClient<long> localFileSystemClient, IFileSystemClient<string> remoteFileSystemClient)
    {
        return new PhotoFileUploader(localFileSystemClient, remoteFileSystemClient, _loggerFactory.CreateLogger<PhotoFileUploader>());
    }
}
