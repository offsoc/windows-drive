using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IThumbnailGenerator
{
    /// <summary>
    /// Obtains a thumbnail for a file.
    /// </summary>
    /// <returns>Thumbnail bytes if thumbnail extraction succeeded; Empty memory otherwise.</returns>
    Task<ReadOnlyMemory<byte>> GenerateThumbnailAsync(
        string filePath,
        int numberOfPixelsOnLargestSide,
        int maxNumberOfBytes,
        CancellationToken cancellationToken);
}
