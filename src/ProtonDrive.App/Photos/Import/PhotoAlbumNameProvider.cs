using System;
using System.IO;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoAlbumNameProvider : IPhotoAlbumNameProvider
{
    private const char AlbumNameSeparator = ' ';

    public (string AlbumName, string AlbumRelativePath) GetAlbumNameFromPath(ReadOnlySpan<char> rootFolderPath, ReadOnlySpan<char> currentFolderPath)
    {
        var rootFolderName = Path.GetFileName(rootFolderPath);

        if (!currentFolderPath.StartsWith(rootFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new PhotoImportException("Album name cannot be provided: import folder path and album folder path are not related");
        }

        if (rootFolderPath.Length == currentFolderPath.Length)
        {
            return (rootFolderName.ToString(), string.Empty);
        }

        var relativePath = currentFolderPath[rootFolderPath.Length..];

        var albumName = string.Create(
            rootFolderName.Length + relativePath.Length,
            new AlbumNameSegments(rootFolderName, relativePath),
            (result, segments) =>
            {
                segments.RootName.CopyTo(result);
                segments.RelativePath.CopyTo(result[segments.RootName.Length..]);
            });

        return (albumName.Replace(Path.DirectorySeparatorChar, AlbumNameSeparator), relativePath[1..].ToString());
    }

    private readonly ref struct AlbumNameSegments(ReadOnlySpan<char> rootName, ReadOnlySpan<char> relativePath)
    {
        public ReadOnlySpan<char> RootName { get; } = rootName;
        public ReadOnlySpan<char> RelativePath { get; } = relativePath;
    }
}
