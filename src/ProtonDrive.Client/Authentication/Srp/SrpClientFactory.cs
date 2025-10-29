using System.Net;
using System.Text;
using ProtonDrive.Client.Authentication.Contracts;

namespace ProtonDrive.Client.Authentication.Srp;

internal sealed class SrpClientFactory : ISrpClientFactory
{
    public ISrpClient Create(NetworkCredential credential, AuthInfo authInfo)
    {
        Span<byte> passwordSpan = stackalloc byte[Encoding.UTF8.GetMaxByteCount(credential.Password.Length)];

        var passwordByteCount = Encoding.UTF8.GetBytes(credential.Password, passwordSpan);

        return new SrpClient(Proton.Cryptography.Srp.SrpClient.Create(
            credential.UserName,
            passwordSpan[..passwordByteCount],
            authInfo.Salt.Span,
            authInfo.Modulus,
            Proton.Cryptography.Srp.SrpClient.GetDefaultModulusVerificationKey()));
    }
}
