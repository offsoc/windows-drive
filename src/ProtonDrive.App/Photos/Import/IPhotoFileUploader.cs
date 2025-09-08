using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoFileUploader
{
    Task<NodeInfo<string>> UploadFileAsync(string filePath, string parentLinkId, string? mainPhotoLinkId, CancellationToken cancellationToken);
}
