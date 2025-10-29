using ProtonDrive.App.Authentication;
using ProtonDrive.Native.Authentication;
using ProtonDrive.Shared.Authentication;

namespace ProtonDrive.App.Windows.Authentication;

internal class Win32Fido2Authenticator : IFido2Authenticator
{
    public bool IsAvailable => WebAuthN.IsAvailable;

    public Task<Fido2AssertionResult> AssertAsync(Fido2AssertionParameters parameters, CancellationToken cancellationToken)
    {
        return WebAuthN.GetAssertionResponseAsync(parameters, cancellationToken: cancellationToken);
    }
}
