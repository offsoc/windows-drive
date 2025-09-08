using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.App.Settings;
using ProtonDrive.Shared;

namespace ProtonDrive.App.Mapping.Setup.PhotoFolders;

internal sealed class PhotoFolderMappingSetupStep
{
    private readonly IPhotosFeatureStateValidator _photosFeatureStateValidator;
    private readonly ILocalFolderSetupAssistant _localFolderSetupAssistant;
    private readonly IRemotePhotoVolumeValidator _remotePhotoVolumeValidator;
    private readonly IRemotePhotoVolumeSetupAssistant _remotePhotoVolumeSetupAssistant;

    public PhotoFolderMappingSetupStep(
        IPhotosFeatureStateValidator photosFeatureStateValidator,
        ILocalFolderSetupAssistant localFolderSetupAssistant,
        IRemotePhotoVolumeValidator remotePhotoVolumeValidator,
        IRemotePhotoVolumeSetupAssistant remotePhotoVolumeSetupAssistant)
    {
        _photosFeatureStateValidator = photosFeatureStateValidator;
        _localFolderSetupAssistant = localFolderSetupAssistant;
        _remotePhotoVolumeValidator = remotePhotoVolumeValidator;
        _remotePhotoVolumeSetupAssistant = remotePhotoVolumeSetupAssistant;
    }

    public async Task<MappingErrorCode> SetUpFoldersAsync(RemoteToLocalMapping mapping, CancellationToken cancellationToken)
    {
        Ensure.IsTrue(mapping.IsPhotoFolderMapping(), "Mapping type has unexpected value", nameof(mapping));

        return
            ValidatePhotosFeatureState() ??
            SetUpLocalFolder(mapping, cancellationToken) ??
            await SetUpRemoteFolderAsync(mapping, cancellationToken).ConfigureAwait(false) ??
            MappingErrorCode.None;
    }

    private MappingErrorCode? ValidatePhotosFeatureState()
    {
        return _photosFeatureStateValidator.Validate();
    }

    private MappingErrorCode? SetUpLocalFolder(RemoteToLocalMapping mapping, CancellationToken cancellationToken)
    {
        return _localFolderSetupAssistant.SetUpLocalFolder(mapping, cancellationToken);
    }

    private async Task<MappingErrorCode?> SetUpRemoteFolderAsync(RemoteToLocalMapping mapping, CancellationToken cancellationToken)
    {
        var replica = mapping.Remote;

        if (replica.IsSetUp())
        {
            return null;
        }

        return
            SetUpPhotoVolumeIdentity(replica) ??
            await CheckPhotoVolumeAccessibilityAsync(replica, cancellationToken).ConfigureAwait(false);
    }

    private MappingErrorCode? SetUpPhotoVolumeIdentity(RemoteReplica replica)
    {
        return _remotePhotoVolumeSetupAssistant.SetUpPhotoVolumeIdentity(replica);
    }

    private Task<MappingErrorCode?> CheckPhotoVolumeAccessibilityAsync(RemoteReplica replica, CancellationToken cancellationToken)
    {
        return _remotePhotoVolumeValidator.CheckAccessibilityAsync(replica, cancellationToken);
    }
}
