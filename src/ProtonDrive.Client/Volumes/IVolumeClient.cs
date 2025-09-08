using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Client.Volumes.Contracts;

namespace ProtonDrive.Client.Volumes;

public interface IVolumeClient
{
    public Task<IReadOnlyCollection<Volume>> GetVolumesAsync(CancellationToken cancellationToken);
    public Task<Volume> CreateMainVolumeAsync(CancellationToken cancellationToken);
    public Task<Volume> CreatePhotoVolumeAsync(CancellationToken cancellationToken);
}
