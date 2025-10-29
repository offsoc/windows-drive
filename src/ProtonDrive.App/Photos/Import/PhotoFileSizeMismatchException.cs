namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoFileSizeMismatchException : PhotoImportException
{
    public PhotoFileSizeMismatchException()
    {
    }

    public PhotoFileSizeMismatchException(string message)
        : base(message)
    {
    }

    public PhotoFileSizeMismatchException(string message, Exception exception)
        : base(message, exception)
    {
    }
}
