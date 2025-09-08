using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

internal sealed class BlockUploadRequestParameters
{
    [JsonPropertyName("AddressID")]
    public required string AddressId { get; init; }

    [JsonPropertyName("LinkID")]
    public required string LinkId { get; init; }

    [JsonPropertyName("RevisionID")]
    public required string RevisionId { get; init; }

    [JsonPropertyName("VolumeID")]
    public required string VolumeId { get; init; }

    [JsonPropertyName("BlockList")]
    public required IReadOnlyCollection<BlockCreationParameters> Blocks { get; init; }

    [JsonPropertyName("ThumbnailList")]
    public required IReadOnlyCollection<ThumbnailCreationParameters> Thumbnails { get; init; }
}
