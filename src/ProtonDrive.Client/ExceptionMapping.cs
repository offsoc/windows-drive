using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using ProtonDrive.Client.Cryptography;
using ProtonDrive.Client.FileUploading;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client;

internal static class ExceptionMapping
{
    public static bool TryMapException(Exception exception, string? id, bool includeObjectId, [MaybeNullWhen(false)] out Exception mappedException)
    {
        mappedException = exception switch
        {
            ApiException ex => CreateFileSystemClientException(ToErrorCode(ex.ResponseCode)),
            CryptographicException => CreateFileSystemClientException(FileSystemErrorCode.Unknown),
            KeyPassphraseUnavailableException => CreateFileSystemClientException(FileSystemErrorCode.Unknown),
            IOException => CreateFileSystemClientException(FileSystemErrorCode.Unknown),
            AggregateException => CreateFileSystemClientException(FileSystemErrorCode.Unknown),
            BlockVerificationFailedException => CreateFileSystemClientException(FileSystemErrorCode.IntegrityFailure),
            _ => null,
        };

        return mappedException is not null;

        FileSystemClientException<string> CreateFileSystemClientException(FileSystemErrorCode errorCode)
        {
            var isMessageAuthoritative = exception is ApiException { Message: not null } or ApiException { IsMessageAuthoritative: true } or IOException;

            return new FileSystemClientException<string>(
                $"{errorCode}",
                errorCode,
                objectId: includeObjectId ? id : default,
                exception)
            {
                // ApiException might contain the error message suitable for displaying in the UI
                IsInnerExceptionMessageAuthoritative = isMessageAuthoritative,
            };
        }
    }

    private static FileSystemErrorCode ToErrorCode(ResponseCode value) => value switch
    {
        ResponseCode.AlreadyExists => FileSystemErrorCode.DuplicateName,
        ResponseCode.DoesNotExist => FileSystemErrorCode.ObjectNotFound,
        ResponseCode.InvalidEncryptedIdFormat => FileSystemErrorCode.ObjectNotFound,
        ResponseCode.InsufficientQuota => FileSystemErrorCode.FreeSpaceExceeded,
        ResponseCode.InsufficientSpace => FileSystemErrorCode.FreeSpaceExceeded,
        ResponseCode.TooManyChildren => FileSystemErrorCode.TooManyChildren,
        ResponseCode.Timeout => FileSystemErrorCode.TimedOut,
        ResponseCode.Offline => FileSystemErrorCode.Offline,
        ResponseCode.InvalidVerificationToken => FileSystemErrorCode.IntegrityFailure,
        ResponseCode.TooManyRequests => FileSystemErrorCode.RateLimited,
        ResponseCode.NetworkError => FileSystemErrorCode.NetworkError,
        ResponseCode.ServerError => FileSystemErrorCode.ServerError,
        ResponseCode.MainPhotoAlreadyInAlbum => FileSystemErrorCode.MainPhotoAlreadyInAlbum,
        _ => FileSystemErrorCode.Unknown,
    };
}
