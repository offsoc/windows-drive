using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoAlbumService : IPhotoAlbumService
{
    private readonly IFileSystemClient<string> _remoteFileSystemClient;
    private readonly ILogger<PhotoAlbumService> _logger;

    public PhotoAlbumService(IFileSystemClient<string> remoteFileSystemClient, ILogger<PhotoAlbumService> logger)
    {
        _remoteFileSystemClient = remoteFileSystemClient;
        _logger = logger;
    }

    public async ValueTask<string> CreateAlbumAsync(string albumName, string parentLinkId, CancellationToken cancellationToken)
    {
        try
        {
            var albumInfo = NodeInfo<string>.Directory()
                .WithName(albumName)
                .WithParentId(parentLinkId);

            var album = await _remoteFileSystemClient.CreateDirectory(albumInfo, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(album.Id))
            {
                throw new FileSystemClientException("Album creation failed: missing ID", FileSystemErrorCode.Unknown);
            }

            _logger.LogInformation("Created album with ID {ID}", album.Id);

            return album.Id;
        }
        catch (FileSystemClientException exception) when (exception.ErrorCode is FileSystemErrorCode.TooManyChildren)
        {
            throw new PhotoAlbumCreationException("Album creation failed: limit reached", exception, PhotoImportErrorCode.MaximumNumberOfAlbumsReached);
        }
    }

    public async ValueTask AddToAlbumAsync(string albumLinkId, IReadOnlyList<NodeInfo<string>> files, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var albumNode = new NodeInfo<string>().WithParentId(albumLinkId);
        await _remoteFileSystemClient.MoveAsync(files, albumNode, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added photo to album with ID {ID}", albumLinkId);
    }
}
