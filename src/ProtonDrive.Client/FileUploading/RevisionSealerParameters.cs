namespace ProtonDrive.Client.FileUploading;

internal readonly record struct RevisionSealerParameters(
    string ShareId,
    string FileId,
    string RevisionId,
    string? ParentLinkId = null);
