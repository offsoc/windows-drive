using System.Text.Json.Serialization;
using ProtonDrive.Client.Authentication.Contracts.Fido2;

namespace ProtonDrive.Client.Authentication.Contracts;

internal sealed class MultiFactorAuthenticationParameters
{
    [JsonPropertyName("Enabled")]
    public MultiFactorAuthenticationMethods Methods { get; init; }

    [JsonPropertyName("FIDO2")]
    public Fido2Challenge? Fido2Challenge { get; init; }
}
