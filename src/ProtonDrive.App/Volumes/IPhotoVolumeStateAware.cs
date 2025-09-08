namespace ProtonDrive.App.Volumes;

public interface IPhotoVolumeStateAware
{
    void OnPhotoVolumeStateChanged(VolumeState value);
}
