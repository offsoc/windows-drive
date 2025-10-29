namespace ProtonDrive.App.Mapping;

internal interface IRootDeletionHandler
{
    void HandleRootDeletion(IEnumerable<int> rootIds);
}
