namespace ProtonDrive.Client.Cryptography.Pgp;

internal interface ISigningCapablePgpDataPacketProducer : IPgpSignatureProducer
{
    Stream GetEncryptingAndSigningStream(ReadOnlyMemory<byte> plainDataSource);

    (Stream EncryptingStream, Stream SignatureStream) GetEncryptingAndSignatureStreams(ReadOnlyMemory<byte> plainDataSource);
}
