using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoFileImporterFactory
{
    public IPhotoFileImporter Create(IPhotoFileSystemClient<long> localFileSystemClient, IFileSystemClient<string> remoteFileSystemClient)
    {
        return new PhotoFileImporter(localFileSystemClient, remoteFileSystemClient);
    }
}
