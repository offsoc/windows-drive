using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AddedPhotoWithLinkIdResponse
{
    [JsonPropertyName("LinkID")]
    public required string LinkId { get; init; }

    public required AddedPhotoResponse Response { get; init; }
}
