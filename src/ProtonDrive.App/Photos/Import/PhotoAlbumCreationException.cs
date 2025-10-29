namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoAlbumCreationException : PhotoImportException
{
    public PhotoAlbumCreationException()
    {
    }

    public PhotoAlbumCreationException(string message)
        : base(message)
    {
    }

    public PhotoAlbumCreationException(string message, Exception exception)
        : base(message, exception)
    {
    }

    public PhotoAlbumCreationException(string message, Exception exception, PhotoImportErrorCode errorCode)
        : base(message, exception)
    {
        ErrorCode = errorCode;
    }
}
