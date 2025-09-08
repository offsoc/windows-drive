using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed class Block
{
    public int Index { get; init; }

    [JsonPropertyName("URL")]
    public string? Url { get; init; }
}
