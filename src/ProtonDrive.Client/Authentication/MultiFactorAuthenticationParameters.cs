using ProtonDrive.Shared.Authentication;

namespace ProtonDrive.Client.Authentication;

public sealed class MultiFactorAuthenticationParameters
{
    public MultiFactorAuthenticationMethods Methods { get; init; }

    public Fido2AssertionParameters? Fido2 { get; init; }
}
