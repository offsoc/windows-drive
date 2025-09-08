using System;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoAlbumNameProvider
{
    (string AlbumName, string AlbumRelativePath) GetAlbumNameFromPath(ReadOnlySpan<char> rootFolderPath, ReadOnlySpan<char> currentFolderPath);
}
