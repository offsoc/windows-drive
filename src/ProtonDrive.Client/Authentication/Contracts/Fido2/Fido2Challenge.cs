using System.Collections.Generic;
using System.Text.Json;

namespace ProtonDrive.Client.Authentication.Contracts.Fido2;

internal sealed class Fido2Challenge
{
    public JsonElement AuthenticationOptions { get; set; }

    public IReadOnlyList<Fido2RegisteredKey> RegisteredKeys { get; set; } = [];
}
