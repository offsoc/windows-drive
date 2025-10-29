using System.Security;
using Proton.Cryptography.Pgp;
using ProtonDrive.Client.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography;

internal interface ICryptographyService
{
    Task<(ISigningCapablePgpMessageProducer Encrypter, Address Address)> CreateMainShareKeyPassphraseEncrypterAsync(CancellationToken cancellationToken);

    Task<(ISigningCapablePgpMessageProducer Encrypter, Address Address)> CreateShareKeyPassphraseEncrypterAsync(
        string mainShareAddressId,
        CancellationToken cancellationToken);

    Task<(ISigningCapablePgpMessageProducer Encrypter, Address SignatureAddress)> CreateNodeNameAndKeyPassphraseEncrypterAsync(
        PgpPublicKey publicKey,
        string signatureAddressId,
        CancellationToken cancellationToken);

    ISigningCapablePgpMessageProducer CreateNodeNameAndKeyPassphraseEncrypter(PgpPublicKey publicKey, PgpPrivateKey signatureKey);

    public ISigningCapablePgpMessageProducer CreateNodeNameAndKeyPassphraseEncrypter(
        PgpPublicKey publicKey,
        PgpSessionKey sessionKey,
        Address signatureAddress);

    Task<(ISigningCapablePgpMessageProducer Encrypter, Address SignatureAddress)> CreateNodeNameAndKeyPassphraseEncrypterAsync(
        PgpPublicKey publicKey,
        PgpSessionKey sessionKey,
        string signatureAddressId,
        CancellationToken cancellationToken);

    ISigningCapablePgpMessageProducer CreateExtendedAttributesEncrypter(PgpPublicKey publicKey, Address signatureAddress);

    ISigningCapablePgpMessageProducer CreateHashKeyEncrypter(PgpPublicKey encryptionKey, PgpPrivateKey signatureKey);

    ISigningCapablePgpDataPacketProducer CreateFileBlockEncrypter(
        PgpSessionKey contentSessionKey,
        PgpPublicKey signaturePublicKey,
        Address signatureAddress);

    public Task<(ISigningCapablePgpDataPacketProducer Encrypter, Address SignatureAddress)> CreateFileBlockEncrypterAsync(
        PgpSessionKey contentSessionKey,
        PgpPublicKey signaturePublicKey,
        string signatureAddressId,
        CancellationToken cancellationToken);

    Task<IVerificationCapablePgpDecrypter> CreateShareKeyPassphraseDecrypterAsync(
        IReadOnlyCollection<string> addressIds,
        string signatureEmailAddress,
        CancellationToken cancellationToken);

    Task<IVerificationCapablePgpDecrypter> CreateNodeNameAndKeyPassphraseDecrypterAsync(
        PgpPrivateKey parentNodeOrShareKey,
        string? signatureEmailAddress,
        CancellationToken cancellationToken);

    IVerificationCapablePgpDecrypter CreateHashKeyDecrypter(PgpPrivateKey privateKey, PgpPublicKey verificationKey);

    Task<IVerificationCapablePgpDecrypter> CreateFileContentsBlockKeyDecrypterAsync(
        PgpPrivateKey nodeKey,
        string? signatureEmailAddress,
        CancellationToken cancellationToken);

    IPgpDecrypter CreateFileContentsBlockDecrypter(PgpPrivateKey nodeKey);

    Task<PgpVerificationStatus> VerifyManifestAsync(
        ReadOnlyMemory<byte> manifest,
        string manifestSignature,
        PgpPrivateKey nodeKey,
        string? signatureEmailAddress,
        CancellationToken cancellationToken);

    PgpPrivateKey GenerateShareOrNodeKey();

    ReadOnlyMemory<byte> GeneratePassphrase();

    (ReadOnlyMemory<byte> KeyPacket, PgpSessionKey SessionKey, string SessionKeySignature) GenerateFileContentKeyPacket(
        PgpPublicKey publicKey,
        PgpPrivateKey signatureKey,
        string? fileName = null);

    ReadOnlyMemory<byte> GenerateHashKey();

    string HashNodeNameHex(byte[] hashKey, string nodeName);

    string HashContentDigestHex(byte[] hashKey, string digest);

    byte[] HashBlockContent(Stream blockContentStream);

    ReadOnlyMemory<byte> DeriveSecretFromPassword(SecureString password, ReadOnlySpan<byte> salt);
}
