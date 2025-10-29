using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AddedPhotoResponseList : ApiResponse
{
    [JsonPropertyName("Responses")]
    public IReadOnlyCollection<AddedPhotoWithLinkIdResponse> AddedPhotoResponses { get; init; } = [];
}
