using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AlbumLinkId([property: JsonPropertyName("LinkID")] string Value);
