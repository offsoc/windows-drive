using System.Text;
using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

internal static class PgpSignatureProducerExtensions
{
    public static string SignWithArmor(
        this IPgpSignatureProducer signatureProducer,
        ReadOnlyMemory<byte> bytes,
        bool encrypted = false)
    {
        using var signatureStream = signatureProducer.GetSignatureStream(bytes, encrypted, PgpEncoding.AsciiArmor);
        using var streamReader = new StreamReader(signatureStream, Encoding.ASCII);
        return streamReader.ReadToEnd();
    }
}
