namespace ProtonDrive.Client.FileUploading;

public sealed record PhotoDuplicate(string FileName, string NameHash, string? ContentHash, bool DraftCreatedByAnotherClient);
