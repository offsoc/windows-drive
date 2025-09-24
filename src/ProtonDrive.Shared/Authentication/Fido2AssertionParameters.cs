using System.Text.Json;

namespace ProtonDrive.Shared.Authentication;

public sealed class Fido2AssertionParameters
{
    public JsonElement AuthenticationOptions { get; init; }
}
