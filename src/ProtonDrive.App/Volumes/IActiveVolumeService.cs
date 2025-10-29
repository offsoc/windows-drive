namespace ProtonDrive.App.Volumes;

internal interface IActiveVolumeService
{
    Task<VolumeInfo?> GetMainVolumeAsync(CancellationToken cancellationToken);
    Task<VolumeInfo> CreateMainVolumeAsync(CancellationToken cancellationToken);
    Task<VolumeInfo?> GetPhotoVolumeAsync(CancellationToken cancellationToken);
    Task<VolumeInfo> CreatePhotoVolumeAsync(CancellationToken cancellationToken);
}
