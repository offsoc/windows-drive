using System.Text.Json.Serialization;
using ProtonDrive.Shared.Text.Serialization;

namespace ProtonDrive.Client.BlockVerification;

internal sealed record VerificationInputResponse
{
    [JsonConverter(typeof(Base64JsonConverter))]
    public ReadOnlyMemory<byte> VerificationCode { get; init; }

    [JsonConverter(typeof(Base64JsonConverter))]
    public ReadOnlyMemory<byte> ContentKeyPacket { get; init; }
}
