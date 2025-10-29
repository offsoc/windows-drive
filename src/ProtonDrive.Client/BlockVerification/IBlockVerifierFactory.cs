using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.BlockVerification;

public interface IBlockVerifierFactory
{
    Task<IBlockVerifier> CreateAsync(string shareId, string linkId, string revisionId, PgpPrivateKey nodeKey, CancellationToken cancellationToken);
}
