using System.Threading.Tasks;

namespace ProtonDrive.App.Volumes;

internal interface IPhotoVolumeService
{
    Task RetryFailedSetupAsync();
}
