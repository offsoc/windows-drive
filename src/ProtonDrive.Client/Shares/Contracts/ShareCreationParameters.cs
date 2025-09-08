using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Shares.Contracts;

internal sealed class ShareCreationParameters
{
    [JsonPropertyName("AddressID")]
    public required string AddressId { get; init; }

    [JsonPropertyName("AddressKeyID")]
    public required string AddressKeyId { get; init; }

    public required string Key { get; init; }
    public required string Passphrase { get; init; }
    public required string PassphraseSignature { get; init; }
}
