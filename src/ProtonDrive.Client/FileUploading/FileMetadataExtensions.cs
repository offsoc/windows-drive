using ProtonDrive.Client.Contracts;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal static class FileMetadataExtensions
{
    public static MediaExtendedAttributes? GetMediaExtendedAttributes(this FileMetadata metadata)
    {
        if (!FileMetadataValidator.IsValidMediaSize(metadata.MediaSize) && metadata.DurationInSeconds is null or 0)
        {
            return null;
        }

        return new MediaExtendedAttributes
        {
            Width = metadata.MediaSize?.Width,
            Height = metadata.MediaSize?.Height,
            Duration = metadata.DurationInSeconds,
        };
    }

    public static GeoLocationExtendedAttributes? GetLocationExtendedAttributes(this FileMetadata metadata)
    {
        if (metadata.Latitude is null || metadata.Longitude is null)
        {
            return null;
        }

        return new GeoLocationExtendedAttributes
        {
            Latitude = metadata.Latitude.Value,
            Longitude = metadata.Longitude.Value,
        };
    }

    public static CameraExtendedAttributes? GetCameraExtendedAttributes(this FileMetadata metadata)
    {
        if ((metadata.CameraOrientation is null or 0) && string.IsNullOrWhiteSpace(metadata.CameraDevice) && metadata.CaptureTime is null)
        {
            return null;
        }

        return new CameraExtendedAttributes
        {
            CaptureTime = metadata.CaptureTime,
            Device = metadata.CameraDevice,
            Orientation = metadata.CameraOrientation,
        };
    }
}
