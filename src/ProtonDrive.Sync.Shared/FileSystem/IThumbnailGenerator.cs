using System;

namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IThumbnailGenerator
{
    public const int MaxThumbnailNumberOfPixelsOnLargestSide = 512;
    public const int MaxHdPreviewNumberOfPixelsOnLargestSide = 1920;

    bool TryGenerateThumbnail(string filePath, int numberOfPixelsOnLargestSide, int maxNumberOfBytes, out ReadOnlyMemory<byte> thumbnailBytes);
}
