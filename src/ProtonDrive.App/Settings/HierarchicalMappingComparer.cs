using System.Collections.Generic;

namespace ProtonDrive.App.Settings;

internal sealed class HierarchicalMappingComparer : IComparer<RemoteToLocalMapping>
{
    private readonly IDictionary<MappingType, int> _mappingTypeHierarchy = new Dictionary<MappingType, int>
    {
        { MappingType.HostDeviceFolder, 1 },
        { MappingType.PhotoImport, 2 },
        { MappingType.PhotoBackup, 3 },
        { MappingType.CloudFiles, 4 },
        { MappingType.ForeignDevice, 5 },
        { MappingType.SharedWithMeRootFolder, 6 },
        { MappingType.SharedWithMeItem, 7 },
    };

    public static HierarchicalMappingComparer Instance { get; } = new();

    public int Compare(RemoteToLocalMapping? x, RemoteToLocalMapping? y)
    {
        if (x is null || y is null)
        {
            return 0;
        }

        if (x.Type != y.Type)
        {
            return _mappingTypeHierarchy[x.Type].CompareTo(_mappingTypeHierarchy[y.Type]);
        }

        return x.Id.CompareTo(y.Id);
    }
}
