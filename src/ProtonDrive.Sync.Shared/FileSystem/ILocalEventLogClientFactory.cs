namespace ProtonDrive.Sync.Shared.FileSystem;

public interface ILocalEventLogClientFactory
{
    IRootableEventLogClient<long> Create();
}
