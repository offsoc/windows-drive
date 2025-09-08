using Microsoft.Extensions.Logging;
using ProtonDrive.App.Photos;

namespace ProtonDrive.App.Mapping.Setup;

internal sealed class PhotosFeatureStateValidator : IPhotosFeatureStateValidator, IPhotosFeatureStateAware
{
    private readonly ILogger<PhotosFeatureStateValidator> _logger;

    private PhotosFeatureState _photosFeatureState = PhotosFeatureState.Idle;

    public PhotosFeatureStateValidator(ILogger<PhotosFeatureStateValidator> logger)
    {
        _logger = logger;
    }

    public MappingErrorCode? Validate()
    {
        var state = _photosFeatureState;

        if (state.Status is not PhotosFeatureStatus.Ready)
        {
            _logger.LogWarning("Photos feature is not ready, status is {PhotosFeatureStatus}", state.Status);

            return state.Status switch
            {
                PhotosFeatureStatus.ReadOnly => MappingErrorCode.PhotosDisabled,
                PhotosFeatureStatus.Disabled => MappingErrorCode.PhotosDisabled,
                PhotosFeatureStatus.Hidden => MappingErrorCode.PhotosDisabled,
                _ => MappingErrorCode.PhotosNotReady,
            };
        }

        return null;
    }

    void IPhotosFeatureStateAware.OnPhotosFeatureStateChanged(PhotosFeatureState value)
    {
        _photosFeatureState = value;
    }
}
