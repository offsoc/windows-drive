using System.Threading.Tasks;

namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IFileMetadataProvider
{
    public Task<FileMetadata?> GetMetadataAsync();
}
