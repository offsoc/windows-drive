using System;

namespace ProtonDrive.App.Photos.Import;

internal sealed class ImportProgressCallbacks
{
    public Action<int, int>? OnProgressChanged { get; init; }

    public Action<PhotoImportFolderCurrentPosition>? OnAlbumCreated { get; init; }
}
