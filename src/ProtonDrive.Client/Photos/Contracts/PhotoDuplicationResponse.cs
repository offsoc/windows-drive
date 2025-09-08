using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Photos.Contracts;

public sealed record PhotoDuplicationResponse : ApiResponse
{
    [JsonPropertyName("DuplicateHashes")]
    public IReadOnlyCollection<PhotoDuplicateDto> PhotoDuplicates { get; init; } = [];
}
