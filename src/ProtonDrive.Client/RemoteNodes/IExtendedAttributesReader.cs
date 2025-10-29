using Proton.Cryptography.Pgp;
using ProtonDrive.Client.Contracts;

namespace ProtonDrive.Client.RemoteNodes;

internal interface IExtendedAttributesReader
{
    Task<ExtendedAttributes?> ReadAsync(Link link, PgpPrivateKey nodeKey, CancellationToken cancellationToken);
}
