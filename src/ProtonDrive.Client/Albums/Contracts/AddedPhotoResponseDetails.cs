using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AddedPhotoResponseDetails
{
    [JsonPropertyName("NewLinkID")]
    public required string NewLinkId { get; init; }
}
