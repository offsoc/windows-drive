using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IThumbnailProvider
{
    public const int MaxThumbnailNumberOfPixelsOnLargestSide = 512;
    public const int MaxHdPreviewNumberOfPixelsOnLargestSide = 1920;

    /// <summary>
    /// Obtains a thumbnail for a file.
    /// </summary>
    /// <returns>Thumbnail bytes if obtaining thumbnail succeeded; Empty memory otherwise.</returns>
    Task<ReadOnlyMemory<byte>> GetThumbnailAsync(int numberOfPixelsOnLargestSide, int maxNumberOfBytes, CancellationToken cancellationToken);
}
