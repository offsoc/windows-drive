namespace ProtonDrive.App.Volumes;

public interface IMainVolumeStateAware
{
    void OnMainVolumeStateChanged(VolumeState value);
}
