using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Mapping;

internal interface IMappingSetupPipeline
{
    Task<MappingState> SetUpAsync(
        RemoteToLocalMapping mapping,
        IReadOnlySet<string> otherLocalSyncFolders,
        CancellationToken cancellationToken);
}
