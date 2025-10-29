using ProtonDrive.Sync.Shared.Trees;

namespace ProtonDrive.App.Sync;

internal interface IMappedFileSystemIdentityProvider
{
    public Task<LooseCompoundAltIdentity<string>?> GetRemoteIdFromLocalIdOrDefaultAsync(
        LooseCompoundAltIdentity<long> localId,
        CancellationToken cancellationToken);

    public Task<LooseCompoundAltIdentity<string>?> GetRemoteIdFromNodeIdOrDefaultAsync(
        long nodeId,
        CancellationToken cancellationToken);
}
