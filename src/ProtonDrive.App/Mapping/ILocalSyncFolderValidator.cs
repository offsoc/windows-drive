namespace ProtonDrive.App.Mapping;

internal interface ILocalSyncFolderValidator
{
    SyncFolderValidationResult? ValidateDrive(string path);
    SyncFolderValidationResult? ValidatePath(string path, IReadOnlySet<string> otherPaths);
    SyncFolderValidationResult? ValidateFolder(string path, bool shouldBeEmpty);
}
