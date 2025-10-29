using System.Security;

namespace ProtonDrive.Client.Cryptography;

internal interface IKeyPassphraseProvider
{
    bool ContainsAtLeastOnePassphrase { get; }
    Task CalculatePassphrasesAsync(SecureString password, CancellationToken cancellationToken);
    void ClearPassphrases();
    ReadOnlyMemory<byte> GetPassphrase(string keyId);
}
