using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoFileImporterFactory
{
    public IPhotoFileUploader Create(IPhotoFileSystemClient<long> localFileSystemClient, IFileSystemClient<string> remoteFileSystemClient)
    {
        return new PhotoFileUploader(localFileSystemClient, remoteFileSystemClient);
    }
}
