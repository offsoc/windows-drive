using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

internal interface ISigningCapablePgpMessageProducer : IPgpMessageProducer, IPgpSignatureProducer
{
    Stream GetEncryptingAndSigningStream(
        ReadOnlyMemory<byte> plainDataSource,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None);

    (Stream EncryptingStream, Stream SignatureStream) GetEncryptingAndSignatureStreams(
        ReadOnlyMemory<byte> plainDataSource,
        bool signatureIsEncrypted,
        PgpEncoding signatureEncoding = PgpEncoding.None,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None);

    (Stream EncryptingStream, Stream SignatureStream, Task<PgpSessionKey> SessionKey) GetEncryptingAndSignatureStreamsWithSessionKey(
        ReadOnlyMemory<byte> plainDataSource,
        bool signatureIsEncrypted,
        PgpEncoding signatureEncoding = PgpEncoding.None,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None);
}
