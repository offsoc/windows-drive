using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Authentication.Contracts.Fido2;

internal sealed class Fido2RegisteredKey
{
    public required string AttestationFormat { get; init; }

    [JsonPropertyName("CredentialID")]
    public required IReadOnlyList<byte> CredentialId { get; init; }

    public required string Name { get; init; }
}
