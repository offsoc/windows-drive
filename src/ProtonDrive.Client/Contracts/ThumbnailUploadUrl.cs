using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed record ThumbnailUploadUrl : UploadUrl
{
    [JsonPropertyName("ThumbnailType")]
    public ThumbnailType Type { get; init; }
}
