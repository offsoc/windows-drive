using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

internal interface IVerificationCapablePgpDecrypter : IPgpDecrypter
{
    (Stream DecryptingStream, Func<PgpVerificationStatus> GetVerificationStatus) GetDecryptingAndVerifyingStream(ReadOnlyMemory<byte> armoredMessage);

    DecryptingAndVerifyingStreamWithSessionKeyProvisionResult GetDecryptingAndVerifyingStreamWithSessionKey(ReadOnlyMemory<byte> armoredMessage);

    DecryptingAndVerifyingStreamWithSessionKeyProvisionResult GetDecryptingAndVerifyingStreamWithSessionKey(
        ReadOnlyMemory<byte> messageSource,
        ReadOnlyMemory<byte> detachedSignatureSource);

    PgpVerificationStatus Verify(ReadOnlySpan<byte> plainData, ReadOnlySpan<byte> armoredSignature);
}
