using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoAlbumService
{
    ValueTask<string> CreateAlbumAsync(string albumName, string parentLinkId, CancellationToken cancellationToken);

    ValueTask AddToAlbumAsync(string albumLinkId, NodeInfo<string> file, CancellationToken cancellationToken);
}
