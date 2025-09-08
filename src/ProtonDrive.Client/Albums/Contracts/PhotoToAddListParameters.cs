using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record PhotoToAddListParameters
{
    [JsonPropertyName("AlbumData")]
    public IReadOnlyCollection<PhotoToAddParameter> Photos { get; init; } = [];
}
