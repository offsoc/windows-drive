namespace ProtonDrive.App.Sync;

public interface IRemoteIdsFromLocalPathProvider
{
    Task<RemoteIds?> GetRemoteIdsOrDefaultAsync(string localPath, CancellationToken cancellationToken);
}
