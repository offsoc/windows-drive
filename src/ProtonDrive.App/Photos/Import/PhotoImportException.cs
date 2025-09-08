using System;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoImportException : Exception
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
            ErrorCode = errorCodeProvider.ErrorCode;
        }
    }

    public FileSystemErrorCode? ErrorCode { get; }
}
