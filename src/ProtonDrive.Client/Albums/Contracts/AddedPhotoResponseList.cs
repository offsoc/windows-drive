using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AddedPhotoResponseList : ApiResponse
{
    [JsonPropertyName("Responses")]
    public IReadOnlyCollection<AddedPhotoResponse> AddedPhotoResponses { get; } = [];
}
