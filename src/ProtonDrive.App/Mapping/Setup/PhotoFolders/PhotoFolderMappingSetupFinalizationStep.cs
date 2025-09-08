using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.App.Photos.Import;
using ProtonDrive.App.Settings;
using ProtonDrive.Shared.Repository;

namespace ProtonDrive.App.Mapping.Setup.PhotoFolders;

internal sealed class PhotoFolderMappingSetupFinalizationStep
{
    private readonly IRepository<PhotoImportSettings> _importRepository;

    public PhotoFolderMappingSetupFinalizationStep(IRepository<PhotoImportSettings> importRepository)
    {
        _importRepository = importRepository;
    }

    public Task<MappingErrorCode> FinishSetupAsync(RemoteToLocalMapping mapping, CancellationToken cancellationToken)
    {
        if (mapping.Type is not MappingType.PhotoImport)
        {
            throw new ArgumentException("Mapping type has unexpected value", nameof(mapping));
        }

        var photoImportFolders = _importRepository.Get()?.Folders ?? [];
        var photoImportFolderStatus = photoImportFolders.FirstOrDefault(x => x.MappingId == mapping.Id)?.Status;
        var result = photoImportFolderStatus switch
        {
            PhotoImportFolderStatus.Failed
                or PhotoImportFolderStatus.SetupFailed
                or PhotoImportFolderStatus.ValidationFailed => MappingErrorCode.PhotoImportFailed,
            _ => MappingErrorCode.None,
        };

        return Task.FromResult(result);
    }
}
