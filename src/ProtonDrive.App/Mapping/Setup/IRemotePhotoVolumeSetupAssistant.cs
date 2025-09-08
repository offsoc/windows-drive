using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Mapping.Setup;

internal interface IRemotePhotoVolumeSetupAssistant
{
    MappingErrorCode? SetUpPhotoVolumeIdentity(RemoteReplica replica);
}
