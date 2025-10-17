using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ProtonDrive.Shared.Logging;
using ProtonDrive.Shared.Media;
using ProtonDrive.Shared.Reporting;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Windows.Services;

internal sealed class Win32ThumbnailGenerationValidator
{
    private const int MinHdPreviewNumberOfPixelsOnLargestSide = IThumbnailProvider.MaxThumbnailNumberOfPixelsOnLargestSide + 1;

    private readonly string _filePath;
    private readonly string _extension;
    private readonly ILogger _logger;
    private readonly IErrorReporting _errorReporting;

    public Win32ThumbnailGenerationValidator(string filePath, string extension, ILogger logger, IErrorReporting errorReporting)
    {
        _filePath = filePath;
        _extension = extension;
        _logger = logger;
        _errorReporting = errorReporting;
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
            var fileExtensionToReport = _extension[^Math.Min(_extension.Length, 5)..];
            _logger.LogWarning(
                "Thumbnail generation failed for file extension \"{fileExtensionToReport}\": failed to determine image dimensions",
                fileExtensionToReport);

            var errorMessage = $"Thumbnail generation failed for file extension \"{fileExtensionToReport}\": failed to determine file size";
            _errorReporting.CaptureError(errorMessage);
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
