using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Settings;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Logging;

namespace ProtonDrive.App.Mapping.SyncFolders;

internal sealed class PhotoFolderService : IPhotoFolderService
{
    private readonly ISyncFolderService _syncFolderService;
    private readonly IMappingRegistry _mappingRegistry;
    private readonly ILogger<PhotoFolderService> _logger;

    public PhotoFolderService(
        ISyncFolderService syncFolderService,
        IMappingRegistry mappingRegistry,
        ILogger<PhotoFolderService> logger)
    {
        _syncFolderService = syncFolderService;
        _mappingRegistry = mappingRegistry;
        _logger = logger;
    }

    public SyncFolderValidationResult ValidateFolder(string path)
    {
        return _syncFolderService.ValidateSyncFolder(path, otherPaths: []);
    }

    public Task AddImportFolderAsync(string path, CancellationToken cancellationToken)
    {
        return AddFolderAsync(path, SyncFolderType.PhotoImport, cancellationToken);
    }

    public Task AddBackupFolderAsync(string path, CancellationToken cancellationToken)
    {
        return AddFolderAsync(path, SyncFolderType.PhotoBackup, cancellationToken);
    }

    public async Task RemoveFolderAsync(SyncFolder folder, CancellationToken cancellationToken)
    {
        Ensure.IsTrue(
            folder.Type is SyncFolderType.PhotoImport or SyncFolderType.PhotoBackup,
            $"Sync folder type must be {SyncFolderType.PhotoImport} or {SyncFolderType.PhotoBackup}",
            nameof(folder));

        using var mappings = await _mappingRegistry.GetMappingsAsync(cancellationToken).ConfigureAwait(false);

        var pathToLog = _logger.GetSensitiveValueForLogging(folder.LocalPath);
        var folderTypeName = GetPhotoFolderTypeName(folder.Type);
        _logger.LogInformation("Requested to remove Photo {PhotoFolderType} folder \"{Path}\"", folderTypeName, pathToLog);

        var mapping = mappings.GetActive().FirstOrDefault(m => m == folder.Mapping);

        if (mapping is null)
        {
            _logger.LogWarning("Unable to find mapping of Photo {PhotoFolderType} folder \"{Path}\"", folderTypeName, pathToLog);
            return;
        }

        mappings.Delete(mapping);
    }

    private static RemoteToLocalMapping CreatePhotoFolderMapping(string path, SyncFolderType folderType)
    {
        return new RemoteToLocalMapping
        {
            Type = ToMappingType(folderType),
            SyncMethod = SyncMethod.Classic,
            Local =
            {
                Path = path,
            },
        };
    }

    private static bool IsMappingOfPhotoFolder(RemoteToLocalMapping mapping, string path)
    {
        return mapping.IsPhotoFolderMapping()
            && string.Equals(mapping.Local.Path, path, StringComparison.OrdinalIgnoreCase);
    }

    private static MappingType ToMappingType(SyncFolderType folderType)
    {
        return folderType switch
        {
            SyncFolderType.PhotoImport => MappingType.PhotoImport,
            SyncFolderType.PhotoBackup => MappingType.PhotoBackup,
            _ => throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null),
        };
    }

    private static string GetPhotoFolderTypeName(SyncFolderType folderType)
    {
        return folderType switch
        {
            SyncFolderType.PhotoImport => "Import",
            SyncFolderType.PhotoBackup => "Backup",
            _ => throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null),
        };
    }

    private async Task AddFolderAsync(string path, SyncFolderType folderType, CancellationToken cancellationToken)
    {
        Ensure.NotNullOrEmpty(path, nameof(path));

        var pathToLog = _logger.GetSensitiveValueForLogging(path);
        var folderTypeName = GetPhotoFolderTypeName(folderType);
        _logger.LogInformation("Requested to add Photo {PhotoFolderType} folder \"{Path}\"", folderTypeName, pathToLog);

        using var mappings = await _mappingRegistry.GetMappingsAsync(cancellationToken).ConfigureAwait(false);

        var activeMappings = mappings.GetActive();

        if (activeMappings.Any(m => IsMappingOfPhotoFolder(m, path)))
        {
            _logger.LogWarning("Ignored Photo {PhotoFolderType} folder \"{Path}\", since it is already mapped", folderTypeName, pathToLog);
            return;
        }

        mappings.Add(CreatePhotoFolderMapping(path, folderType));
    }
}
