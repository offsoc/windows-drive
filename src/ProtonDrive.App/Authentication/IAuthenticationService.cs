using System.Net;
using System.Security;

namespace ProtonDrive.App.Authentication;

public interface IAuthenticationService
{
    Task AuthenticateAsync(NetworkCredential credential);
    Task AuthenticateWithTotpAsync(string secondFactor);
    Task AuthenticateWithFido2Async();
    Task FinishTwoPasswordAuthenticationAsync(SecureString secondPassword);
    Task CancelAuthenticationAsync();
    void RestartAuthentication();
}
