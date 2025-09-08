using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ProtonDrive.Shared.Media;
using ProtonDrive.Sync.Shared.FileSystem;
using Gdi32 = ProtonDrive.App.Windows.Interop.Gdi32;
using Shell32 = ProtonDrive.App.Windows.Interop.Shell32;

namespace ProtonDrive.App.Windows.Services;

internal class Win32ThumbnailGenerator : IThumbnailGenerator
{
    private const int MinHdPreviewNumberOfPixelsOnLargestSide = IThumbnailProvider.MaxThumbnailNumberOfPixelsOnLargestSide + 1;

    // Below 15 it will start becoming unrecognizable and maybe even so ugly that using an icon rather than a thumbnail is likely to be more acceptable.
    private static readonly ImmutableArray<int> QualityLevels = [80, 70, 60, 45, 30, 15, 10, 5];

    private readonly ILogger<IThumbnailGenerator> _logger;

    public Win32ThumbnailGenerator(ILogger<IThumbnailGenerator> logger)
    {
        _logger = logger;
    }

    public bool TryGenerateThumbnail(string filePath, int numberOfPixelsOnLargestSide, int maxNumberOfBytes, out ReadOnlyMemory<byte> thumbnailBytes)
    {
        var extension = Path.GetExtension(filePath);

        if (!KnownFileExtensions.ImageExtensions.Contains(extension) && !KnownFileExtensions.VideoExtensions.Contains(extension))
        {
            _logger.LogInformation("Thumbnail generation skipped: file extension not supported");
            thumbnailBytes = ReadOnlyMemory<byte>.Empty;
            return false;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Thumbnail generation failed: File not found");
            thumbnailBytes = ReadOnlyMemory<byte>.Empty;
            return false;
        }

        IntPtr hBitmap = IntPtr.Zero;

        try
        {
            var isHdPreview = IsRequestingHdPreview(numberOfPixelsOnLargestSide);

            if (isHdPreview)
            {
                var validator = new HdPreviewGenerationValidator(filePath, extension, _logger);

                if (!validator.IsHdPreviewAllowed())
                {
                    thumbnailBytes = ReadOnlyMemory<byte>.Empty;
                    return false;
                }
            }

            hBitmap = GetNativeBitmapHandle(filePath, numberOfPixelsOnLargestSide);

            if (hBitmap == IntPtr.Zero)
            {
                thumbnailBytes = ReadOnlyMemory<byte>.Empty;
                return false;
            }

            var bitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            var nonTransparentBitmap = GetNonTransparentBitmap(bitmap);

            var qualityLevelIndex = 0;
            do
            {
                thumbnailBytes = EncodeToJpeg(nonTransparentBitmap, QualityLevels[qualityLevelIndex++]);
            }
            while (thumbnailBytes.Length > maxNumberOfBytes && qualityLevelIndex < QualityLevels.Length);

            if (thumbnailBytes.Length > maxNumberOfBytes)
            {
                throw new ThumbnailGenerationException($"Could not generate thumbnail of less than {maxNumberOfBytes} bytes.");
            }

            _logger.LogDebug(
                "{ThumbnailType} generation succeeded for file \"{FileName}\": {Width}x{Height} pixels, {Size} bytes",
                isHdPreview ? "HD preview" : "Thumbnail",
                Path.GetFileName(filePath),
                bitmap.PixelWidth,
                bitmap.PixelHeight,
                thumbnailBytes.Length);

            return true;
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Thumbnail generation failed: File not found");
            thumbnailBytes = ReadOnlyMemory<byte>.Empty;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail generation failed: {ExceptionType}: {HResult}", ex.GetType().Name, ex.HResult);
            thumbnailBytes = ReadOnlyMemory<byte>.Empty;
            return false;
        }
        finally
        {
            Gdi32.DeleteObject(hBitmap);
        }
    }

    private static bool IsRequestingHdPreview(int numberOfPixelsOnLargestSide)
    {
        return numberOfPixelsOnLargestSide > IThumbnailProvider.MaxThumbnailNumberOfPixelsOnLargestSide;
    }

    private static RenderTargetBitmap GetNonTransparentBitmap(BitmapSource bitmap)
    {
        var rect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
        var visual = new DrawingVisual();
        var context = visual.RenderOpen();
        context.DrawRectangle(new SolidColorBrush(Colors.Black), null, rect);
        context.DrawImage(bitmap, rect);
        context.Close();
        var render = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        render.Render(visual);
        return render;
    }

    private static byte[] EncodeToJpeg(BitmapSource source, int qualityLevel)
    {
        using var stream = new MemoryStream();

        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = qualityLevel,
            Frames = { BitmapFrame.Create(source) },
        };

        encoder.Save(stream);

        return stream.ToArray();
    }

    private IntPtr GetNativeBitmapHandle(string fileName, int numberOfPixelsOnLargestSide)
    {
        var itemGuid = Shell32.IID_IShellItem;

        var resultHandle = Shell32.SHCreateItemFromParsingName(fileName, IntPtr.Zero, ref itemGuid, out object item);

        resultHandle.ThrowOnFailure();

        try
        {
            var shellItem = (Shell32.IShellItem)item;

            var thumbnailCache = Shell32.ThumbnailCache.GetInstance();

            try
            {
                resultHandle = thumbnailCache.GetThumbnail(
                    shellItem,
                    (uint)numberOfPixelsOnLargestSide,
                    Shell32.WTS_FLAGS.WTS_EXTRACT | Shell32.WTS_FLAGS.WTS_EXTRACTDONOTCACHE | Shell32.WTS_FLAGS.WTS_SCALETOREQUESTEDSIZE,
                    out var sharedBitmap,
                    out _,
                    out _);

                if (resultHandle.Failed)
                {
                    _logger.LogWarning("Thumbnail generation failed: 0x{ErrorCode:x8}", resultHandle.AsInt32);

                    return IntPtr.Zero;
                }

                resultHandle = sharedBitmap.Detach(out var detachedBitmap);
                resultHandle.ThrowOnFailure();

                return detachedBitmap;
            }
            finally
            {
                Marshal.ReleaseComObject(thumbnailCache);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(item);
        }
    }

    private sealed class HdPreviewGenerationValidator
    {
        private readonly string _filePath;
        private readonly string _extension;
        private readonly ILogger _logger;

        public HdPreviewGenerationValidator(string filePath, string extension, ILogger logger)
        {
            _filePath = filePath;
            _extension = extension;
            _logger = logger;
        }

        public bool IsHdPreviewAllowed()
        {
            if (!KnownFileExtensions.ImageExtensions.Contains(_extension))
            {
                _logger.LogInformation("HD preview generation skipped: file extension not supported");
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
                        "HD preview generation skipped: JPEG image too small (largest side smaller or equal than {RequiredSize}",
                        IThumbnailProvider.MaxHdPreviewNumberOfPixelsOnLargestSide);

                    return false;
                }

                return true;
            }

            if (imageNumberOfPixelsOnLargestSide < MinHdPreviewNumberOfPixelsOnLargestSide)
            {
                _logger.LogInformation(
                    "HD preview generation skipped: non JPEG image too small (largest side smaller than {RequiredSize}",
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
                _logger.LogWarning("HD preview generation failed: file format not supported");
                numberOfPixelsOnLargestSide = 0;
                return false;
            }
        }
    }
}
