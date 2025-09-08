using System.Diagnostics.CodeAnalysis;

namespace ProtonDrive.Client.FileUploading;

public sealed class PhotoNameCollision(string linkId, string fileName, string nameHash, string? contentHash, bool draftCreatedByAnotherClient)
{
    private readonly bool _draftCreatedByAnotherClient = draftCreatedByAnotherClient;
    private readonly string? _contentHash = contentHash;

    public string LinkId { get; } = linkId;
    public string FileName { get; } = fileName;
    public string NameHash { get; } = nameHash;

    public bool TryGetContentHashIfNotDraft([MaybeNullWhen(false)] out string contentHash, [NotNullWhen(false)] out bool? draftCreatedByAnotherClient)
    {
        if (_contentHash is null)
        {
            contentHash = null;
            draftCreatedByAnotherClient = _draftCreatedByAnotherClient;
            return false;
        }

        contentHash = _contentHash;
        draftCreatedByAnotherClient = false;
        return true;
    }
}
