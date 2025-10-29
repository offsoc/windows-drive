using CommunityToolkit.HighPerformance;
using Proton.Cryptography.Pgp;
using ProtonDrive.Client.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography;

internal class KeyBasedPgpDecrypter(IReadOnlyList<PgpPrivateKey> privateKeyRing) : IPgpDecrypter
{
    protected PgpPrivateKeyRing PgpPrivateKeyRing { get; } = new(privateKeyRing);

    public PgpSessionKey DecryptSessionKey(ReadOnlyMemory<byte> keyPacket)
    {
        return PgpPrivateKeyRing.DecryptSessionKey(keyPacket.Span);
    }

    public Stream GetDecryptingStream(Stream messageSource)
    {
        return PgpPrivateKeyRing.OpenDecryptingStream(messageSource);
    }

    public (Stream Stream, PgpSessionKey SessionKey) GetDecryptingStreamWithSessionKey(ReadOnlyMemory<byte> armoredMessage)
    {
        var message = PgpArmorDecoder.Decode(armoredMessage.Span);
        var sessionKey = PgpPrivateKeyRing.DecryptSessionKey(message);
        var stream = PgpEncryptingStream.OpenRead(armoredMessage.AsStream(), sessionKey, PgpEncoding.AsciiArmor);
        return (stream, sessionKey);
    }
}
