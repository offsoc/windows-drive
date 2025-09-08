using System;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal class PhotoImportException : Exception
{
    public PhotoImportException()
    {
    }

    public PhotoImportException(string message)
        : base(message)
    {
    }

    public PhotoImportException(string message, Exception exception)
        : base(message, exception)
    {
        if (exception is IFileSystemErrorCodeProvider errorCodeProvider)
        {
            ErrorCode = GetPhotoImportErrorCode(errorCodeProvider.ErrorCode);
        }
    }

    public PhotoImportErrorCode ErrorCode { get; protected set; }

    private static PhotoImportErrorCode GetPhotoImportErrorCode(FileSystemErrorCode errorCode)
    {
        return errorCode switch
        {
            FileSystemErrorCode.ObjectNotFound => PhotoImportErrorCode.AlbumDoesNotExist,
            FileSystemErrorCode.TooManyChildren => PhotoImportErrorCode.MaximumNumberOfPhotosPerAlbumReached,
            _ => PhotoImportErrorCode.Unknown,
        };
    }
}
