using CommunityToolkit.HighPerformance;
using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

public sealed class SigningCapablePgpMessageProducer : PgpSignatureProducer, ISigningCapablePgpMessageProducer
{
    private readonly PgpPublicKey _publicKey;
    private readonly PgpSessionKey? _sessionKey;
    private readonly PgpPrivateKey _signaturePrivateKey;

    public SigningCapablePgpMessageProducer(PgpPublicKey publicKey, PgpPrivateKey signaturePrivateKey)
        : base(signaturePrivateKey, publicKey)
    {
        _publicKey = publicKey;
        _signaturePrivateKey = signaturePrivateKey;
    }

    public SigningCapablePgpMessageProducer(PgpPublicKey publicKey, PgpSessionKey sessionKey, PgpPrivateKey signaturePrivateKey)
        : this(publicKey, signaturePrivateKey)
    {
        _sessionKey = sessionKey;
    }

    public Stream GetEncryptingStream(
        ReadOnlyMemory<byte> plainDataSource,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None)
    {
        var encryptionSecrets = _sessionKey is not null ? new EncryptionSecrets(_publicKey, _sessionKey.Value) : new EncryptionSecrets(_publicKey);
        return PgpEncryptingStream.OpenRead(plainDataSource.AsStream(), encryptionSecrets, outputEncoding, compression);
    }

    public (Stream Stream, Task<PgpSessionKey> SessionKey) GetEncryptingStreamWithSessionKey(
        ReadOnlyMemory<byte> plainDataSource,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None)
    {
        var sessionKey = _sessionKey ?? PgpSessionKey.Generate();
        var stream = PgpEncryptingStream.OpenRead(plainDataSource.AsStream(), new EncryptionSecrets(_publicKey, sessionKey), outputEncoding, compression);
        return (stream, Task.FromResult(sessionKey));
    }

    public Stream GetEncryptingAndSigningStream(
        ReadOnlyMemory<byte> plainDataSource,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None)
    {
        var encryptionSecrets = _sessionKey is not null ? new EncryptionSecrets(_publicKey, _sessionKey.Value) : new EncryptionSecrets(_publicKey);
        return PgpEncryptingStream.OpenRead(plainDataSource.AsStream(), encryptionSecrets, SigningKeyRing, outputEncoding, compression);
    }

    public (Stream EncryptingStream, Stream SignatureStream) GetEncryptingAndSignatureStreams(
        ReadOnlyMemory<byte> plainDataSource,
        bool signatureIsEncrypted,
        PgpEncoding signatureEncoding = PgpEncoding.None,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None)
    {
        var encryptionSecrets = _sessionKey is not null ? new EncryptionSecrets(_publicKey, _sessionKey.Value) : new EncryptionSecrets(_publicKey);

        var signatureStream = new MemoryStream();
        Stream signingOutputStream = signatureStream;
        Stream? signingStreamToDispose = null;

        if (signatureIsEncrypted)
        {
            signingOutputStream = PgpEncryptingStream.Open(signatureStream, _signaturePrivateKey, signatureEncoding);
            signingStreamToDispose = signingOutputStream;
        }

        Stream encryptingStream = PgpEncryptingStream.OpenRead(
            plainDataSource.AsStream(),
            signingOutputStream,
            encryptionSecrets,
            SigningKeyRing,
            outputEncoding,
            compression);

        if (signingStreamToDispose is not null)
        {
            encryptingStream = new DisposingStreamDecorator(encryptingStream, signingStreamToDispose);
        }

        return (encryptingStream, signatureStream);
    }

    public (Stream EncryptingStream, Stream SignatureStream, Task<PgpSessionKey> SessionKey) GetEncryptingAndSignatureStreamsWithSessionKey(
        ReadOnlyMemory<byte> plainDataSource,
        bool signatureIsEncrypted,
        PgpEncoding signatureEncoding = PgpEncoding.None,
        PgpEncoding outputEncoding = PgpEncoding.None,
        PgpCompression compression = PgpCompression.None)
    {
        var sessionKey = _sessionKey ?? PgpSessionKey.Generate();

        var signatureStream = new MemoryStream();
        Stream signingOutputStream = signatureStream;
        Stream? signingStreamToDispose = null;

        if (signatureIsEncrypted)
        {
            signingOutputStream = PgpEncryptingStream.Open(signatureStream, _signaturePrivateKey, signatureEncoding);
            signingStreamToDispose = signingOutputStream;
        }

        var encryptionSecrets = new EncryptionSecrets(_publicKey, sessionKey);
        Stream encryptingStream = PgpEncryptingStream.OpenRead(
            plainDataSource.AsStream(),
            signingOutputStream,
            encryptionSecrets,
            SigningKeyRing,
            outputEncoding,
            compression);

        if (signingStreamToDispose is not null)
        {
            encryptingStream = new DisposingStreamDecorator(encryptingStream, signingStreamToDispose);
        }

        return (encryptingStream, signatureStream, Task.FromResult(sessionKey));
    }
}
