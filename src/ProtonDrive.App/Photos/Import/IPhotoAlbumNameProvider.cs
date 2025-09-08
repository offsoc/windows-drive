using System;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoAlbumNameProvider
{
    string GetAlbumNameFromPath(ReadOnlySpan<char> rootFolderPath, ReadOnlySpan<char> relativeFolderPath);
}
