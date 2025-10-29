using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Mapping.Setup;

internal interface ILocalFolderValidationStep
{
    Task<MappingErrorCode> ValidateAsync(RemoteToLocalMapping mapping, IReadOnlySet<string> otherLocalSyncFolders, CancellationToken cancellationToken);
}
