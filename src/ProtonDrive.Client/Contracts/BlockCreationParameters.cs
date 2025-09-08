using System;
using System.Text.Json.Serialization;
using ProtonDrive.BlockVerification;

namespace ProtonDrive.Client.Contracts;

internal sealed class BlockCreationParameters : BlockCreationParametersBase
{
    public BlockCreationParameters(int index, int size, string encryptedSignature, ReadOnlyMemory<byte> hash, VerificationToken? verificationToken)
    {
        Index = index;
        Size = size;
        EncryptedSignature = encryptedSignature;
        Hash = hash;
        VerifierOutput = verificationToken is not null ? new BlockVerifierOutput(verificationToken.Value.AsReadOnlyMemory()) : null;
    }

    public int Index { get; }

    [JsonPropertyName("EncSignature")]
    public string EncryptedSignature { get; }

    [JsonPropertyName("Verifier")]
    public BlockVerifierOutput? VerifierOutput { get; }
}
