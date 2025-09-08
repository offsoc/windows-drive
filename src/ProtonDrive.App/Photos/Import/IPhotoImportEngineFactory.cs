using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoImportEngineFactory
{
    IPhotoImportEngine CreateEngine(RemoteToLocalMapping mapping, PhotoImportFolderCurrentPosition? currentPosition);
}
