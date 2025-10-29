using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.RemoteNodes;

internal interface IPrivateKeyHolder
{
    PgpPrivateKey PrivateKey { get; }
}
