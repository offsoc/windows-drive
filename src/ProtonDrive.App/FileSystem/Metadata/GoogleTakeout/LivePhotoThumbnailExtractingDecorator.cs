using System;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.App.Photos.LivePhoto;
using ProtonDrive.Sync.Shared.FileSystem;

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

    public async Task<ReadOnlyMemory<byte>> GenerateThumbnailAsync(
        string filePath,
        int numberOfPixelsOnLargestSide,
        int maxNumberOfBytes,
        CancellationToken cancellationToken)
    {
        var thumbnail = await _decoratedInstance.GenerateThumbnailAsync(
            filePath,
            numberOfPixelsOnLargestSide,
            maxNumberOfBytes,
            cancellationToken).ConfigureAwait(false);

        if (!thumbnail.IsEmpty)
        {
            return thumbnail;
        }

        if (_livePhotoFileDetector.TryGetMainLivePhotoPath(filePath, out var relatedPhotoFilePath))
        {
            return await _decoratedInstance.GenerateThumbnailAsync(
                relatedPhotoFilePath,
                numberOfPixelsOnLargestSide,
                maxNumberOfBytes,
                cancellationToken).ConfigureAwait(false);
        }

        return thumbnail;
    }
}
