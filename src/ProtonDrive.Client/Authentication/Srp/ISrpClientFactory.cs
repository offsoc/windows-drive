using System.Net;
using ProtonDrive.Client.Authentication.Contracts;

namespace ProtonDrive.Client.Authentication.Srp;

internal interface ISrpClientFactory
{
    ISrpClient Create(NetworkCredential credential, AuthInfo authInfo);
}
