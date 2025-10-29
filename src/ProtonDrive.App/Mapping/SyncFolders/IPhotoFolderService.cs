using ProtonDrive.App.Photos.Import;

namespace ProtonDrive.App.Mapping.SyncFolders;

public interface IPhotoFolderService
{
    /// <summary>
    /// Validates local folder applicability for importing or backing up photos from.
    /// </summary>
    /// <remarks>
    /// It checks whether:
    /// <list type="bullet">
    /// <item>The folder exists</item>
    /// <item>The folder path does not overlap with already synced folders</item>
    /// <item>The folder is on a supported volume</item>
    /// </list>
    /// </remarks>
    /// <param name="path">The path of a Photo import ir backup folder to validate.</param>
    /// <returns>The result of the validation.</returns>
    SyncFolderValidationResult ValidateFolder(string path);

    /// <summary>
    /// Adds a mapping for the specified local folder to enable Photo import from it.
    /// </summary>
    /// <remarks>
    /// No validation of folder is attempted, it will be performed by sync folder mapping setup.
    /// </remarks>
    /// <param name="path">Local folder path.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous adding mapping operation.
    /// It's optional to await it as the method doesn't raise expected exceptions.
    /// </returns>
    Task AddImportFolderAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Reset the mapping and the photo import folder status to trigger a new import attempt.
    /// </summary>
    /// <param name="mappingId">Mapping ID related to the photo folder to retry to import</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous adding mapping operation.</returns>
    Task ResetImportFolderStatusAsync(int mappingId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a mapping for the specified local folder to enable Photo backup from it.
    /// </summary>
    /// <remarks>
    /// No validation of folder is attempted, it will be performed by sync folder mapping setup.
    /// </remarks>
    /// <param name="path">Local folder path.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous adding mapping operation.
    /// It's optional to await it as the method doesn't raise expected exceptions.
    /// </returns>
    Task AddBackupFolderAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Removes mapping of the specified photo import or backup folder.
    /// </summary>
    /// <param name="folder">The photo folder to remove.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous removing mapping operation.
    /// It's optional to await it as the method doesn't raise expected exceptions.
    /// </returns>
    Task RemoveFolderAsync(SyncFolder folder, CancellationToken cancellationToken);

    /// <summary>
    /// Provides the settings of all photo import folders.
    /// </summary>
    /// <returns>The settings of all photo import folders.</returns>
    PhotoImportSettings GetSettings();

    /// <summary>
    /// Update the settings of all photo import folders.
    /// </summary>
    /// <param name="settings">The new settings of all photo import folders.</param>
    void SetSettings(PhotoImportSettings settings);
}
