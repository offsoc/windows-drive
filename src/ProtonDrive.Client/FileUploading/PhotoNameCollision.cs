namespace ProtonDrive.Client.FileUploading;

public sealed record PhotoNameCollision(string LinkId, string FileName, string NameHash, string? ContentHash);
