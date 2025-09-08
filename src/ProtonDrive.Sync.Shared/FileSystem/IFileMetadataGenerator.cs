using System.Threading.Tasks;

namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IFileMetadataGenerator
{
    Task<FileMetadata?> GetMetadataAsync(string filePath);
}
