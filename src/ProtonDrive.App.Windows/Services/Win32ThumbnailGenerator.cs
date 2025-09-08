using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Media;
using ProtonDrive.Shared.Threading;
using ProtonDrive.Sync.Shared.FileSystem;
using Gdi32 = ProtonDrive.App.Windows.Interop.Gdi32;
using Shell32 = ProtonDrive.App.Windows.Interop.Shell32;

namespace ProtonDrive.App.Windows.Services;

internal class Win32ThumbnailGenerator : IThumbnailGenerator
{
    private static readonly TimeSpan ThumbnailExtractionTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ThumbnailExtractionResultPollingInterval = TimeSpan.FromMilliseconds(500);

    // Below 15 it will start becoming unrecognizable and maybe even so ugly that using an icon rather than a thumbnail is likely to be more acceptable.
    private static readonly ImmutableArray<int> QualityLevels = [80, 70, 60, 45, 30, 15, 10, 5];

    // Polling for pending thumbnail retrieval result must be performed on the same thread on which thumbnail retrieval was requested.
    private static readonly DedicatedThreadTaskScheduler TaskScheduler = new();

    private readonly IClock _clock;
    private readonly ILogger<IThumbnailGenerator> _logger;

    public Win32ThumbnailGenerator(IClock clock, ILogger<IThumbnailGenerator> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public async Task<ReadOnlyMemory<byte>> GenerateThumbnailAsync(string filePath, int numberOfPixelsOnLargestSide, int maxNumberOfBytes, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath);

        if (!KnownFileExtensions.ImageExtensions.Contains(extension) && !KnownFileExtensions.VideoExtensions.Contains(extension))
        {
            _logger.LogInformation(
                "Thumbnail generation skipped: File extension \"{Extension}\" not supported",
                extension[^Math.Min(extension.Length, 5)..]);

            return ReadOnlyMemory<byte>.Empty;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Thumbnail generation failed: File not found");
            return ReadOnlyMemory<byte>.Empty;
        }

        IntPtr hBitmap = IntPtr.Zero;

        try
        {
            var isHdPreview = IsRequestingHdPreview(numberOfPixelsOnLargestSide);

            if (isHdPreview)
            {
                var validator = new Win32ThumbnailGenerationValidator(filePath, extension, _logger);

                if (!validator.IsHdPreviewAllowed())
                {
                    return ReadOnlyMemory<byte>.Empty;
                }
            }

            hBitmap = await GetNativeBitmapHandleAsync(filePath, numberOfPixelsOnLargestSide, cancellationToken).ConfigureAwait(false);

            if (hBitmap == IntPtr.Zero)
            {
                return ReadOnlyMemory<byte>.Empty;
            }

            var bitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            var nonTransparentBitmap = bitmap.GetNonTransparentBitmap();

            var qualityLevelIndex = 0;
            byte[] thumbnailBytes;
            do
            {
                thumbnailBytes = nonTransparentBitmap.EncodeToJpeg(QualityLevels[qualityLevelIndex++]);
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

            return thumbnailBytes;
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Thumbnail generation failed: File not found");
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Thumbnail generation failed for file extension \"{Extension}\": {ExceptionType}: {HResult}",
                extension[^Math.Min(extension.Length, 5)..],
                ex.GetType().Name,
                ex.HResult);

            return ReadOnlyMemory<byte>.Empty;
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

    private static Task<TResult> Schedule<TResult>(Func<TResult> work, CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            work,
            cancellationToken,
            TaskCreationOptions.None,
            TaskScheduler);
    }

    private async Task<IntPtr> GetNativeBitmapHandleAsync(string fileName, int numberOfPixelsOnLargestSide, CancellationToken cancellationToken)
    {
        var itemGuid = Shell32.IID_IShellItem;

        var resultHandle = Shell32.SHCreateItemFromParsingName(fileName, IntPtr.Zero, ref itemGuid, out object item);

        resultHandle.ThrowOnFailure();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var shellItem = (Shell32.IShellItem)item;

            var thumbnailCache = Shell32.ThumbnailCache.GetInstance();

            try
            {
                return await GetThumbnailHandleAsync(thumbnailCache, shellItem, numberOfPixelsOnLargestSide, cancellationToken).ConfigureAwait(false);
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

    private async Task<IntPtr> GetThumbnailHandleAsync(
        Shell32.IThumbnailCache thumbnailCache,
        Shell32.IShellItem shellItem,
        int numberOfPixelsOnLargestSide,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startTickCount = _clock.TickCount;

        var (result, thumbnail) = await Schedule(() => thumbnailCache.TryGetThumbnail(shellItem, numberOfPixelsOnLargestSide), cancellationToken).ConfigureAwait(false);

        while (result.ThumbnailExtractionIsPending() && (_clock.TickCount - startTickCount) < ThumbnailExtractionTimeout)
        {
            await Task.Delay(ThumbnailExtractionResultPollingInterval, cancellationToken).ConfigureAwait(false);

            (result, thumbnail) = await Schedule(() => thumbnailCache.TryGetDeferredThumbnail(shellItem, numberOfPixelsOnLargestSide), cancellationToken).ConfigureAwait(false);
        }

        if (result.Failed || thumbnail is null)
        {
            _logger.LogWarning("Thumbnail generation of max {Size}px failed: 0x{ErrorCode:x8}", numberOfPixelsOnLargestSide, result.AsInt32);

            return IntPtr.Zero;
        }

        var resultHandle = thumbnail.Detach(out var nativeBitmapHandle);
        resultHandle.ThrowOnFailure();

        return nativeBitmapHandle;
    }
}
