using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public record UploadUrl
{
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("BareURL")]
    public string BareUrl { get; init; } = string.Empty;
}
