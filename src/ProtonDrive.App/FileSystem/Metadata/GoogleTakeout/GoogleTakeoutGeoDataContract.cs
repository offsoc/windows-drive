using System.Text.Json.Serialization;

namespace ProtonDrive.App.FileSystem.Metadata.GoogleTakeout;

internal sealed class GoogleTakeoutGeoDataContract
{
    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; init; }
}
