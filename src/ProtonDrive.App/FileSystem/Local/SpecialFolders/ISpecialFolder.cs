using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Local.SpecialFolders;

internal interface ISpecialFolder<TId>
    where TId : IEquatable<TId>
{
    Task<NodeInfo<TId>> GetOrCreate(CancellationToken cancellationToken);
}
