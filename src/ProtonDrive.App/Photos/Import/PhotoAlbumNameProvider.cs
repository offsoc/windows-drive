using System;
using System.IO;
using ProtonDrive.Shared.IO;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoAlbumNameProvider : IPhotoAlbumNameProvider
{
    private const char NameSeparatorCharacter = ' ';
    private const char NameSuffixCharacter = '~';

    public string GetAlbumNameFromPath(ReadOnlySpan<char> rootFolderPath, ReadOnlySpan<char> relativeFolderPath)
    {
        if (relativeFolderPath.IsEmpty)
        {
            return GetDisplayName(rootFolderPath).ToString();
        }

        var takeoutRelativePath = GetGoogleTakeoutRelativePath(rootFolderPath, relativeFolderPath);
        if (!takeoutRelativePath.IsEmpty)
        {
            return GetAlbumName(takeoutRelativePath);
        }

        var rootFolderName = GetDisplayName(rootFolderPath);

        return GetAlbumName(rootFolderName, relativeFolderPath);
    }

    private static ReadOnlySpan<char> GetGoogleTakeoutRelativePath(ReadOnlySpan<char> rootPath, ReadOnlySpan<char> relativePath)
    {
        const string takeoutGooglePhotos = @"Takeout\Google Photos";
        const string takeout = @"\Takeout";
        const string googlePhotos = @"Google Photos\";

        if (rootPath.EndsWith(Path.DirectorySeparatorChar))
        {
            rootPath = rootPath[..^1];
        }

        var position = relativePath.IndexOf(takeoutGooglePhotos, StringComparison.OrdinalIgnoreCase);

        // Root folder is ancestor of "Takeout\Google Photos"
        if (position > 0 &&
            relativePath[position - 1] == Path.DirectorySeparatorChar &&
            relativePath.Length > position + takeoutGooglePhotos.Length &&
            relativePath[position + takeoutGooglePhotos.Length] == Path.DirectorySeparatorChar)
        {
            return relativePath[(position + takeoutGooglePhotos.Length + 1)..];
        }

        // Root folder is parent of "Takeout\Google Photos"
        if (position == 0 &&
            relativePath.Length > takeoutGooglePhotos.Length &&
            relativePath[takeoutGooglePhotos.Length] == Path.DirectorySeparatorChar)
        {
            return relativePath[(takeoutGooglePhotos.Length + 1)..];
        }

        // Root folder is "Takeout", nested folder is "Google Photos"
        if (rootPath.EndsWith(takeout, StringComparison.OrdinalIgnoreCase) &&
            relativePath.StartsWith(googlePhotos, StringComparison.OrdinalIgnoreCase) &&
            relativePath.Length > googlePhotos.Length)
        {
            return relativePath[googlePhotos.Length..];
        }

        // Root folder is "Takeout\Google Photos"
        if (rootPath.Length > takeoutGooglePhotos.Length &&
            rootPath.EndsWith(takeoutGooglePhotos, StringComparison.OrdinalIgnoreCase) &&
            rootPath[^(takeoutGooglePhotos.Length + 1)] == Path.DirectorySeparatorChar)
        {
            return relativePath;
        }

        // Not a Google Takeout
        return ReadOnlySpan<char>.Empty;
    }

    private static string GetAlbumName(ReadOnlySpan<char> rootFolderName, ReadOnlySpan<char> relativePath)
    {
        return string.Create(
            rootFolderName.Length + 1 + relativePath.Length,
            new AlbumNameSegments(rootFolderName, relativePath),
            (result, segments) =>
            {
                segments.RootName.CopyTo(result);
                result[segments.RootName.Length] = NameSeparatorCharacter;
                segments.RelativePath.CopyTo(result[(segments.RootName.Length + 1)..]);
                result.Replace(Path.DirectorySeparatorChar, NameSeparatorCharacter);
            });
    }

    private static string GetAlbumName(ReadOnlySpan<char> relativePath)
    {
        return string.Create(
            relativePath.Length,
            relativePath,
            (result, param) =>
            {
                param.CopyTo(result);
                result.Replace(Path.DirectorySeparatorChar, NameSeparatorCharacter);
            });
    }

    private static ReadOnlySpan<char> GetDisplayName(ReadOnlySpan<char> path)
    {
        var displayName = PathExtensions.GetDisplayNameWithoutAccess(path);

        if (displayName.EndsWith(Path.VolumeSeparatorChar))
        {
            displayName = displayName[..^1];
        }

        if (displayName.IsEmpty)
        {
            displayName = [NameSuffixCharacter];
        }

        return displayName;
    }

    private readonly ref struct AlbumNameSegments(ReadOnlySpan<char> rootName, ReadOnlySpan<char> relativePath)
    {
        public ReadOnlySpan<char> RootName { get; } = rootName;
        public ReadOnlySpan<char> RelativePath { get; } = relativePath;
    }
}
