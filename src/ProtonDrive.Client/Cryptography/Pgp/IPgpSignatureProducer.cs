using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

internal interface IPgpSignatureProducer
{
    Stream GetSignatureStream(ReadOnlyMemory<byte> plainDataSource, bool signatureIsEncrypted, PgpEncoding signatureEncoding = PgpEncoding.None);
}
