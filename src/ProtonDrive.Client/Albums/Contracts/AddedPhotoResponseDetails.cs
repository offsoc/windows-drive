using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AddedPhotoResponseDetails
{
    [JsonPropertyName("NewLinkID")]
    public string? NewLinkId { get; init; }
}
