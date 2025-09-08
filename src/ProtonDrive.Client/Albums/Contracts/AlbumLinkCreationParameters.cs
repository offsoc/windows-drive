using System.Text.Json.Serialization;
using ProtonDrive.Client.Contracts;

namespace ProtonDrive.Client.Albums.Contracts;

public sealed class AlbumLinkCreationParameters : FolderCreationParameters
{
    [JsonPropertyName("SignatureEmail")]
    public new string? SignatureEmailAddress { get; set; }

    public static AlbumLinkCreationParameters FromFolderCreationParameters(FolderCreationParameters parameters)
    {
        return new AlbumLinkCreationParameters
        {
            Name = parameters.Name,
            NameHash = parameters.NameHash,
            NodePassphrase = parameters.NodePassphrase,
            NodePassphraseSignature = parameters.NodePassphraseSignature,
            SignatureEmailAddress = parameters.SignatureEmailAddress,
            NodeKey = parameters.NodeKey,
            NodeHashKey = parameters.NodeHashKey,
        };
    }
}
