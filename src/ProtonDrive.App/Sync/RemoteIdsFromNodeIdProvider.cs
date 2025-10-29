using ProtonDrive.App.Mapping;
using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Sync;

internal sealed class RemoteIdsFromNodeIdProvider : IRemoteIdsFromNodeIdProvider, IMappingsAware
{
    private readonly IMappedFileSystemIdentityProvider _mappedFileSystemIdentityProvider;

    private IReadOnlyCollection<RemoteToLocalMapping> _activeMappings = [];

    public RemoteIdsFromNodeIdProvider(IMappedFileSystemIdentityProvider mappedFileSystemIdentityProvider)
    {
        _mappedFileSystemIdentityProvider = mappedFileSystemIdentityProvider;
    }

    void IMappingsAware.OnMappingsChanged(IReadOnlyCollection<RemoteToLocalMapping> activeMappings, IReadOnlyCollection<RemoteToLocalMapping> deletedMappings)
    {
        _activeMappings = activeMappings;
    }

    public async Task<RemoteIds?> GetRemoteIdsOrDefaultAsync(int mappingId, long remoteNodeId, CancellationToken cancellationToken)
    {
        var mapping = _activeMappings.FirstOrDefault(m => m.Id == mappingId);

        if (mapping is not { Remote: { VolumeId: not null, ShareId: not null } })
        {
            return null;
        }

        var remoteId = await _mappedFileSystemIdentityProvider
            .GetRemoteIdFromNodeIdOrDefaultAsync(remoteNodeId, cancellationToken)
            .ConfigureAwait(false);

        return remoteId?.ItemId is not null ? new RemoteIds(mapping.Remote.VolumeId, mapping.Remote.ShareId, LinkId: remoteId.Value.ItemId) : null;
    }
}
