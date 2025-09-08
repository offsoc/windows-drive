using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoFileImporter
{
    Task<NodeInfo<string>> ImportFileAsync(string filePath, string parentLinkId, CancellationToken cancellationToken);
}
