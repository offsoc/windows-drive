using ProtonDrive.Shared.Authentication;

namespace ProtonDrive.App.Authentication;

public interface IFido2Authenticator
{
    bool IsAvailable { get; }

    Task<Fido2AssertionResult> AssertAsync(Fido2AssertionParameters parameters, CancellationToken cancellationToken);
}
