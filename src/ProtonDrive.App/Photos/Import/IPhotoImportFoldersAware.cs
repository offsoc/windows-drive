using ProtonDrive.App.Mapping.SyncFolders;

namespace ProtonDrive.App.Photos.Import;

public interface IPhotoImportFoldersAware
{
    void OnPhotoImportFolderChanged(SyncFolderChangeType changeType, PhotoImportFolderState folder);
}
