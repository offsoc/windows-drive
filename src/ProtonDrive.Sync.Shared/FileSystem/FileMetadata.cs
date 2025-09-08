using System;
using System.Drawing;

namespace ProtonDrive.Sync.Shared.FileSystem;

public sealed record FileMetadata(
    Size? MediaSize,
    double? DurationInSeconds,
    int? CameraOrientation,
    string? CameraDevice,
    DateTimeOffset? CaptureTime,
    double? Latitude,
    double? Longitude);
