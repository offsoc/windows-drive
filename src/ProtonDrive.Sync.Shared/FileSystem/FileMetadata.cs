using System.Drawing;

namespace ProtonDrive.Sync.Shared.FileSystem;

public sealed record FileMetadata
{
    public static FileMetadata Empty { get; } = new();

    public Size? MediaSize { get; init; }
    public double? DurationInSeconds { get; init; }
    public int? CameraOrientation { get; init; }
    public string? CameraDevice { get; init; }
    public DateTimeOffset? CaptureTime { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
