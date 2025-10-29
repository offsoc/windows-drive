namespace ProtonDrive.Client.Contracts;

public sealed class CameraExtendedAttributes
{
    public DateTimeOffset? CaptureTime { get; set; }

    public string? Device { get; set; }

    public int? Orientation { get; set; }
}
