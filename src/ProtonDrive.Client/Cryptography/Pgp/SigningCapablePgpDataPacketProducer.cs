using CommunityToolkit.HighPerformance;
using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

public sealed class SigningCapablePgpDataPacketProducer : PgpSignatureProducer, ISigningCapablePgpDataPacketProducer
{
    private readonly PgpSessionKey _sessionKey;

    public SigningCapablePgpDataPacketProducer(
        PgpSessionKey sessionKey,
        PgpPrivateKeyRing signingKeyRing,
        PgpPublicKey? signatureEncryptionKey = null)
        : base(signingKeyRing, signatureEncryptionKey)
    {
        _sessionKey = sessionKey;
    }

    public Stream GetEncryptingAndSigningStream(ReadOnlyMemory<byte> plainDataSource)
    {
        return PgpEncryptingStream.OpenRead(plainDataSource.AsStream(), _sessionKey, SigningKeyRing);
    }

    public (Stream EncryptingStream, Stream SignatureStream) GetEncryptingAndSignatureStreams(ReadOnlyMemory<byte> plainDataSource)
    {
        var signatureStream = new MemoryStream();

        var signatureOutputStream = SignatureEncryptionKey?.OpenEncryptingStream(signatureStream, PgpEncoding.AsciiArmor)
            ?? (Stream)PgpArmorEncodingStream.Open(signatureStream, PgpBlockType.Signature);

        Stream encryptingStream = PgpEncryptingStream.OpenRead(plainDataSource.AsStream(), signatureOutputStream, _sessionKey, SigningKeyRing);

        encryptingStream = new DisposingStreamDecorator(encryptingStream, signatureOutputStream);

        return (encryptingStream, signatureStream);
    }
}
