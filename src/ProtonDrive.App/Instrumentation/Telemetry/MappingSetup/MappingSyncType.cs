namespace ProtonDrive.App.Instrumentation.Telemetry.MappingSetup;

public enum MappingSyncType
{
    /// <summary>
    /// For photo-import mappings, there is no synchronization.
    /// </summary>
    None,

    /// <summary>
    /// From remote to local sync. It is used for read-only "shared with me" items.
    /// </summary>
    OneWayToLocal,

    /// <summary>
    /// Default two-way synchronization.
    /// </summary>
    TwoWay,
}
