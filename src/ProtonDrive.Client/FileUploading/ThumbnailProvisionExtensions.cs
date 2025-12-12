using Proton.Drive.Sdk.Nodes;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal static class ThumbnailProvisionExtensions
{
    public static async Task<IEnumerable<Thumbnail>> GetThumbnailsAsync(this IThumbnailProvider thumbnailProvider, CancellationToken cancellationToken)
    {
        var smallThumbnail = await thumbnailProvider.GetSmallThumbnailAsync(cancellationToken).ConfigureAwait(false);

        if (smallThumbnail is null)
        {
            return [];
        }

        var largeThumbnail = await thumbnailProvider.GetPreviewThumbnailAsync(cancellationToken).ConfigureAwait(false);

        return largeThumbnail is null ? [smallThumbnail] : [smallThumbnail, largeThumbnail];
    }

    private static async Task<Thumbnail?> GetSmallThumbnailAsync(this IThumbnailProvider thumbnailProvider, CancellationToken cancellationToken)
    {
        const int maxNumberOfBytes = Constants.MaxSmallThumbnailSizeOnRemote - Constants.MaxThumbnailEncryptionOverhead;

        var thumbnail = await thumbnailProvider
            .TryGetThumbnailAsync(IThumbnailProvider.MaxThumbnailNumberOfPixelsOnLargestSide, maxNumberOfBytes, cancellationToken)
            .ConfigureAwait(false);

        return thumbnail?.Length > 0 ? new Thumbnail(ThumbnailType.Thumbnail, thumbnail.Value) : null;
    }

    private static async Task<Thumbnail?> GetPreviewThumbnailAsync(this IThumbnailProvider thumbnailProvider, CancellationToken cancellationToken)
    {
        const int maxNumberOfBytes = Constants.MaxLargeThumbnailSizeOnRemote - Constants.MaxThumbnailEncryptionOverhead;

        var thumbnail = await thumbnailProvider
            .TryGetThumbnailAsync(IThumbnailProvider.MaxHdPreviewNumberOfPixelsOnLargestSide, maxNumberOfBytes, cancellationToken)
            .ConfigureAwait(false);

        return thumbnail?.Length > 0 ? new Thumbnail(ThumbnailType.Preview, thumbnail.Value) : null;
    }
}
