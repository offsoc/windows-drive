namespace ProtonDrive.App.Photos.Import;

public readonly struct PhotoImportFolderCurrentPosition
{
    public required string AlbumLinkId { get; init; }
    public required string RelativePath { get; init; }
}
