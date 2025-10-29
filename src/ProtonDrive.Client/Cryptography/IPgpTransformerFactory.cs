using Proton.Cryptography.Pgp;
using ProtonDrive.Client.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography;

internal interface IPgpTransformerFactory
{
    ISigningCapablePgpMessageProducer CreateMessageAndSignatureProducingEncrypter(
        PgpPublicKey publicKey,
        PgpPrivateKey signaturePrivateKey);

    ISigningCapablePgpMessageProducer CreateMessageAndSignatureProducingEncrypter(
        PgpPublicKey publicKey,
        PgpSessionKey sessionKey,
        PgpPrivateKey signaturePrivateKey);

    IPgpDecrypter CreateDecrypter(IReadOnlyList<PgpPrivateKey> privateKeyRing);

    IVerificationCapablePgpDecrypter CreateVerificationCapableDecrypter(
        IReadOnlyList<PgpPrivateKey> privateKeyRing,
        IReadOnlyList<PgpPublicKey> verificationPublicKeyRing);
}
