using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Photos.Import;
using ProtonDrive.App.Settings;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Logging;
using ProtonDrive.Shared.Repository;

namespace ProtonDrive.App.Mapping.SyncFolders;

internal sealed class PhotoFolderService : IPhotoFolderService
{
    private readonly ISyncFolderService _syncFolderService;
    private readonly IMappingRegistry _mappingRegistry;
    private readonly IRepository<PhotoImportSettings> _settingsRepository;
    private readonly ILogger<PhotoFolderService> _logger;

    public PhotoFolderService(
        ISyncFolderService syncFolderService,
        IMappingRegistry mappingRegistry,
        IRepository<PhotoImportSettings> settingsRepository,
        ILogger<PhotoFolderService> logger)
    {
        _syncFolderService = syncFolderService;
        _mappingRegistry = mappingRegistry;
        _settingsRepository = settingsRepository;
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

    public async Task ResetImportFolderStatusAsync(int mappingId, CancellationToken cancellationToken)
    {
        using var mappings = await _mappingRegistry.GetMappingsAsync(cancellationToken).ConfigureAwait(false);

        var mapping = mappings.GetActive().FirstOrDefault(x => x.Id == mappingId);

        if (mapping is null)
        {
            _logger.LogWarning("Ignored Photo import folder status reset: ID not found");
            return;
        }

        if (mapping.Type is not MappingType.PhotoImport)
        {
            _logger.LogWarning("Ignored Photo import folder status reset: invalid folder type {FolderType}", mapping.Type);
            return;
        }

        var photoImportSettings = GetSettings();
        var photoImportFolderToReset = photoImportSettings.Folders.FirstOrDefault(x => x.MappingId == mappingId);

        if (photoImportFolderToReset is null)
        {
            _logger.LogWarning("Ignored Photo import folder status reset: could not find folder for mapping ID {MappingId}", mappingId);
            return;
        }

        var pathToLog = _logger.GetSensitiveValueForLogging(photoImportFolderToReset.Path);
        photoImportFolderToReset.Status = PhotoImportFolderStatus.NotStarted;
        SetSettings(photoImportSettings);
        _logger.LogInformation("Status of photo import folder \"{Path}\" reset to {Status}", pathToLog, PhotoImportFolderStatus.NotStarted);

        mapping.Status = MappingStatus.New;
        mappings.Update(mapping);
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

    public PhotoImportSettings GetSettings()
    {
        return _settingsRepository.Get() ?? new PhotoImportSettings([]);
    }

    public void SetSettings(PhotoImportSettings settings)
    {
        _settingsRepository.Set(settings);
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
