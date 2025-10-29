using ProtonDrive.Sync.Shared.FileSystem;
using ProtonDrive.Sync.Shared.FileSystem.Photos;

namespace ProtonDrive.App.FileSystem.Metadata.GoogleTakeout;

public sealed class LivePhotoThumbnailExtractingDecorator : IThumbnailGenerator
{
    private readonly IThumbnailGenerator _decoratedInstance;
    private readonly ILivePhotoFileDetector _livePhotoFileDetector;

    public LivePhotoThumbnailExtractingDecorator(IThumbnailGenerator instanceToDecorate, ILivePhotoFileDetector livePhotoFileDetector)
    {
        _decoratedInstance = instanceToDecorate;
        _livePhotoFileDetector = livePhotoFileDetector;
    }

    public async Task<ReadOnlyMemory<byte>?> TryGenerateThumbnailAsync(
        string filePath,
        int numberOfPixelsOnLargestSide,
        int maxNumberOfBytes,
        CancellationToken cancellationToken)
    {
        var thumbnail = await _decoratedInstance.TryGenerateThumbnailAsync(
            filePath,
            numberOfPixelsOnLargestSide,
            maxNumberOfBytes,
            cancellationToken).ConfigureAwait(false);

        if (thumbnail != null)
        {
            return thumbnail;
        }

        if (_livePhotoFileDetector.TryGetMainLivePhotoPath(filePath, out var relatedPhotoFilePath))
        {
            return await _decoratedInstance.TryGenerateThumbnailAsync(
                relatedPhotoFilePath,
                numberOfPixelsOnLargestSide,
                maxNumberOfBytes,
                cancellationToken).ConfigureAwait(false);
        }

        return thumbnail;
    }
}
