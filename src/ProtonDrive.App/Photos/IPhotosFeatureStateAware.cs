namespace ProtonDrive.App.Photos;

public interface IPhotosFeatureStateAware
{
    void OnPhotosFeatureStateChanged(PhotosFeatureState value);
}
