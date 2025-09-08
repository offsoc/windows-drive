using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed record FolderCreationResponse : ApiResponse
{
    private readonly FolderId? _folderId;

    [JsonPropertyName("Folder")]
    public FolderId FolderId
    {
        get => _folderId ?? throw new ApiException("Folder ID is not set");
        init => _folderId = value;
    }
}
