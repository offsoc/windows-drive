using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AlbumShortDto([property: JsonPropertyName("Link")] AlbumLinkId LinkId);
