using System.Security;
using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

public interface IPgpKeyPacketProducer
{
    ReadOnlyMemory<byte> GetKeyPacket(PgpPublicKey publicKey);
    ReadOnlyMemory<byte> GetKeyPacket(SecureString password);
}
