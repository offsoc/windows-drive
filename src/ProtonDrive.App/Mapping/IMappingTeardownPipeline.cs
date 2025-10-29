using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Mapping;

internal interface IMappingTeardownPipeline
{
    Task<MappingState> TearDownAsync(RemoteToLocalMapping mapping, CancellationToken cancellationToken);
}
