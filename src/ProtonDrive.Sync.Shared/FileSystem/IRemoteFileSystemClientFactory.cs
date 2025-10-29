namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IRemoteFileSystemClientFactory
{
    IFileSystemClient<string> CreateClient(FileSystemClientParameters parameters);
}
