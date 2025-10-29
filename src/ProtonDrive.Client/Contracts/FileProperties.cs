using System.Text.Json.Serialization;
using ProtonDrive.Shared.Text.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed class FileProperties
{
    [JsonConverter(typeof(Base64JsonConverter))]
    public ReadOnlyMemory<byte> ContentKeyPacket { get; init; }

    public string? ContentKeyPacketSignature { get; init; }

    public RevisionHeader? ActiveRevision { get; init; }
}
