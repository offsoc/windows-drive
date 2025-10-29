namespace ProtonDrive.Client.BlockVerification;

public interface IBlockVerifier
{
    VerificationToken VerifyBlock(ReadOnlyMemory<byte> dataPacketPrefix, ReadOnlySpan<byte> plainDataPrefix);
}
