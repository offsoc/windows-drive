using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Sync.Windows.FileSystem.Client;

public sealed class LocalEventLogClientFactory : ILocalEventLogClientFactory
{
    public IRootableEventLogClient<long> Create()
    {
        return new EventLogClient();
    }
}
