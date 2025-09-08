using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ProtonDrive.Shared.Logging;
using ProtonDrive.Shared.Media;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Windows.Services;

internal sealed class Win32ThumbnailGenerationValidator
{
    private const int MinHdPreviewNumberOfPixelsOnLargestSide = IThumbnailProvider.MaxThumbnailNumberOfPixelsOnLargestSide + 1;

    private readonly string _filePath;
    private readonly string _extension;
    private readonly ILogger _logger;

    public Win32ThumbnailGenerationValidator(string filePath, string extension, ILogger logger)
    {
        _filePath = filePath;
        _extension = extension;
        _logger = logger;
    }

    public bool IsHdPreviewAllowed()
    {
        if (!KnownFileExtensions.ImageExtensions.Contains(_extension))
        {
            _logger.LogInformation(
                "HD preview generation skipped: File extension \"{Extension}\" not supported",
                _extension[^Math.Min(_extension.Length, 5)..]);

            return false;
        }

        if (!TryGetNumberOfPixelsOnLargestSide(out var imageNumberOfPixelsOnLargestSide))
        {
            return false;
        }

        if (KnownFileExtensions.JpegExtensions.Contains(_extension))
        {
            if (imageNumberOfPixelsOnLargestSide <= IThumbnailProvider.MaxHdPreviewNumberOfPixelsOnLargestSide)
            {
                _logger.LogInformation(
                    "HD preview generation skipped: JPEG image too small (largest side smaller or equal than {RequiredSize})",
                    IThumbnailProvider.MaxHdPreviewNumberOfPixelsOnLargestSide);

                return false;
            }

            return true;
        }

        if (imageNumberOfPixelsOnLargestSide < MinHdPreviewNumberOfPixelsOnLargestSide)
        {
            _logger.LogInformation(
                "HD preview generation skipped: Non JPEG image too small (largest side smaller than {RequiredSize})",
                MinHdPreviewNumberOfPixelsOnLargestSide);

            return false;
        }

        return true;
    }

    private bool TryGetNumberOfPixelsOnLargestSide(out int numberOfPixelsOnLargestSide)
    {
        try
        {
            using var stream = File.OpenRead(_filePath);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            numberOfPixelsOnLargestSide = Math.Max(decoder.Frames[0].PixelWidth, decoder.Frames[0].PixelHeight);
            return true;
        }
        catch (NotSupportedException)
        {
            var filenameToLog = _logger.GetSensitiveValueForLogging(Path.GetFileName(_filePath));
            _logger.LogWarning(
                "HD preview generation for file \"{File}\" with extension \"{Extension}\" failed: File format not supported",
                filenameToLog,
                _extension[^Math.Min(_extension.Length, 5)..]);

            numberOfPixelsOnLargestSide = 0;
            return false;
        }
    }
}
