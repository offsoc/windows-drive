using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.Cryptography;

public sealed record AddressKey(string Id, PgpPrivateKey PrivateKey, bool IsAllowedForEncryption, bool PrivateKeyIsUnlocked);
