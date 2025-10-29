using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

public class PgpSignatureProducer : IPgpSignatureProducer
{
    public PgpSignatureProducer(PgpPrivateKeyRing signingKeyRing, PgpPublicKey? signatureEncryptionKey = null)
    {
        SigningKeyRing = signingKeyRing;
        SignatureEncryptionKey = signatureEncryptionKey;
    }

    protected PgpPrivateKeyRing SigningKeyRing { get; }
    protected PgpPublicKey? SignatureEncryptionKey { get; }

    public Stream GetSignatureStream(ReadOnlyMemory<byte> plainDataSource, bool signatureIsEncrypted, PgpEncoding signatureEncoding = PgpEncoding.None)
    {
        var signatureStream = new MemoryStream();
        Stream signingOutputStream = signatureIsEncrypted && SignatureEncryptionKey is not null
            ? SignatureEncryptionKey.Value.OpenEncryptingStream(signatureStream)
            : signatureStream;

        using (var signingStream = PgpSigningStream.Open(signingOutputStream, SigningKeyRing, signatureEncoding))
        {
            signingStream.Write(plainDataSource.Span);
        }

        signatureStream.Seek(0, SeekOrigin.Begin);
        return signatureStream;
    }
}
