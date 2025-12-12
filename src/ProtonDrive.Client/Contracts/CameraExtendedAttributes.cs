using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed class CameraExtendedAttributes
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CaptureTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Device { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Orientation { get; set; }
}
