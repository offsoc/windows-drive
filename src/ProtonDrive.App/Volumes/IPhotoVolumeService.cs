namespace ProtonDrive.App.Volumes;

internal interface IPhotoVolumeService
{
    Task RetryFailedSetupAsync();
}
