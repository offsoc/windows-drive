using System.Collections.Generic;

namespace ProtonDrive.App.Photos.Import;

public sealed record PhotoImportSettings(IList<PhotoImportFolderState> Folders);
