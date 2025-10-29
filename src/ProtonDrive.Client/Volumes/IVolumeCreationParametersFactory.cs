using ProtonDrive.Client.Volumes.Contracts;

namespace ProtonDrive.Client.Volumes;

internal interface IVolumeCreationParametersFactory
{
    Task<VolumeCreationParameters> CreateForMainVolumeAsync(CancellationToken cancellationToken);
    Task<PhotoVolumeCreationParameters> CreateForPhotoVolumeAsync(CancellationToken cancellationToken);
}
