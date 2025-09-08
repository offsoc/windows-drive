using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Sync.Windows.FileSystem.Photos;

public interface IPhotoTagsGenerator
{
    Task<IReadOnlySet<PhotoTag>> GetPhotoTagsAsync(string filePath, CancellationToken cancellationToken);
}
