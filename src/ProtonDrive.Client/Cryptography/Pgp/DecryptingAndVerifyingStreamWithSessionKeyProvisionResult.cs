using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography.Pgp;

public record DecryptingAndVerifyingStreamWithSessionKeyProvisionResult(
    Stream DecryptionStream,
    PgpSessionKey SessionKey,
    Func<PgpVerificationStatus> GetVerificationStatus);
