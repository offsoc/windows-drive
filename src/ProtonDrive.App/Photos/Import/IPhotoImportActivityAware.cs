using ProtonDrive.Sync.Shared.SyncActivity;

namespace ProtonDrive.App.Photos.Import;

public interface IPhotoImportActivityAware
{
    /// <summary>
    /// Occurs when a photo file has been imported or failed to import.
    /// </summary>
    /// <param name="item">The item that is affected by the change.</param>
    void OnPhotoImportActivityChanged(SyncActivityItem<long> item);
}
