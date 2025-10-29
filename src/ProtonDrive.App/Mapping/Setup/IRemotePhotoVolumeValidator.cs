using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Mapping.Setup;

internal interface IRemotePhotoVolumeValidator
{
    MappingErrorCode? ValidateIdentity(RemoteReplica replica);
    Task<MappingErrorCode?> CheckAccessibilityAsync(RemoteReplica replica, CancellationToken cancellationToken);
}
