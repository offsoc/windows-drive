using Microsoft.Extensions.Logging;
using ProtonDrive.App.Settings;
using ProtonDrive.App.Volumes;
using ProtonDrive.Shared;

namespace ProtonDrive.App.Mapping.Setup;

internal sealed class RemotePhotoVolumeSetupAssistant : IRemotePhotoVolumeSetupAssistant, IPhotoVolumeStateAware
{
    private readonly VolumeIdentityProvider _volumeIdentityProvider;
    private readonly ILogger<RemotePhotoVolumeSetupAssistant> _logger;

    private VolumeState _photoVolumeState = VolumeState.Idle;

    public RemotePhotoVolumeSetupAssistant(
        VolumeIdentityProvider volumeIdentityProvider,
        ILogger<RemotePhotoVolumeSetupAssistant> logger)
    {
        _volumeIdentityProvider = volumeIdentityProvider;
        _logger = logger;
    }

    public MappingErrorCode? SetUpPhotoVolumeIdentity(RemoteReplica replica)
    {
        var (status, volume, _) = _photoVolumeState;

        if (status is not VolumeStatus.Ready)
        {
            _logger.LogWarning("Photo volume is not ready, status is {VolumeStatus}", status);

            return MappingErrorCode.PhotosNotReady;
        }

        Ensure.NotNull(volume, nameof(volume));

        replica.VolumeId = volume.Id;
        replica.ShareId = volume.RootShareId;
        replica.RootLinkId = volume.RootLinkId;
        replica.InternalVolumeId = _volumeIdentityProvider.GetRemoteVolumeId(replica.VolumeId);

        return null;
    }

    void IPhotoVolumeStateAware.OnPhotoVolumeStateChanged(VolumeState value)
    {
        _photoVolumeState = value;
    }
}
