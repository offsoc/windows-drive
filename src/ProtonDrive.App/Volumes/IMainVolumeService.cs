using System.Threading.Tasks;

namespace ProtonDrive.App.Volumes;

public interface IMainVolumeService
{
    VolumeState State { get; }

    Task<VolumeInfo?> GetVolumeAsync();
}
