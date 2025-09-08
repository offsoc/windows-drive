using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client;

public sealed class FetchLinksMetadataParameters
{
    [JsonPropertyName("LinkIDs")]
    public required IReadOnlyList<string> LinkIds { get; init; } = [];
}
