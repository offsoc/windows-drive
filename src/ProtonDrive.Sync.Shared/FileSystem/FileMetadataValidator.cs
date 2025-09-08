using System;
using System.Drawing;

namespace ProtonDrive.Sync.Shared.FileSystem;

public static class FileMetadataValidator
{
    private static readonly DateTimeOffset MinCaptureTime = new(new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    public static bool IsValidMediaSize(Size? mediaSize)
    {
        return mediaSize is { Width: > 0, Height: > 0 };
    }

    public static bool IsValidMediaSize(int? widthInPixels, int? heightInPixels)
    {
        return widthInPixels > 0 && heightInPixels > 0;
    }

    public static bool IsValidCaptureTime(DateTimeOffset? captureTime)
    {
        return captureTime > MinCaptureTime;
    }

    public static bool IsValidDuration(double? durationInSeconds)
    {
        return durationInSeconds > 0;
    }

    public static bool IsValidCameraOrientation(int? cameraOrientation)
    {
        return cameraOrientation >= 0;
    }

    public static bool IsValidCameraDevice(string? cameraDevice)
    {
        return !string.IsNullOrWhiteSpace(cameraDevice);
    }

    public static bool IsValidGeoCoordinates(double? latitude, double? longitude)
    {
        return latitude is not null && Math.Abs(latitude.Value) <= 90 && longitude is not null && Math.Abs(longitude.Value) <= 180;
    }

    public static bool IsValid(
        int? widthInPixels,
        int? heightInPixels,
        double? durationInSeconds,
        int? cameraOrientation,
        string? cameraDevice,
        double? latitude,
        double? longitude)
    {
        return IsValidMediaSize(widthInPixels, heightInPixels)
            || durationInSeconds > 0
            || cameraOrientation > 0
            || !string.IsNullOrWhiteSpace(cameraDevice)
            || (latitude is not null && longitude is not null);
    }
}
