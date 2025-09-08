namespace ProtonDrive.Shared.Telemetry;

public enum ErrorScope
{
    /// <summary>
    /// Error related to the sync pass
    /// </summary>
    Sync,

    /// <summary>
    /// Error related to the synchronisation of the item
    /// </summary>
    ItemOperation,

    /// <summary>
    /// Error occurred when attempting to open a Proton Doc, Sheet, or other Proton document
    /// </summary>
    DocumentOpening,

    /// <summary>
    /// Error occurred when attempting to sanitize a Proton document file by adding the file extension
    /// </summary>
    DocumentNameMigration,

    /// <summary>
    /// Error occurred when attempting to set up a mapping folder
    /// </summary>
    MappingSetup,

    /// <summary>
    /// Error occured when attempting to enable or disable local storage optimization
    /// </summary>
    StorageOptimization,
}
