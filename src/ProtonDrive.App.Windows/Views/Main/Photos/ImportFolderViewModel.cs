using CommunityToolkit.Mvvm.ComponentModel;
using ProtonDrive.App.Mapping;
using ProtonDrive.App.Mapping.SyncFolders;
using ProtonDrive.App.Photos.Import;
using ProtonDrive.App.Windows.Views.Shared;
using ProtonDrive.Shared;

namespace ProtonDrive.App.Windows.Views.Main.Photos;

internal sealed class ImportFolderViewModel : ObservableObject, IMappingStatusViewModel
{
    private MappingSetupStatus _status;
    private MappingErrorCode _errorCode = MappingErrorCode.None;
    private PhotoImportFolderStatus _importStatus;
    private int? _numberOfFilesToImport;
    private int _numberOfImportedFiles;
    private string? _errorMessage;
    private bool _importIsCompleted;
    private bool _noPhotosFound;

    public ImportFolderViewModel(string name, SyncFolder syncFolder)
    {
        Path = syncFolder.LocalPath;
        Name = name;
        SyncFolder = syncFolder;
        Status = syncFolder.Status;
    }

    public ImportFolderViewModel(string path, string name, SyncFolderValidationResult validationResult)
    {
        Path = path;
        Name = name;
        ValidationResult = validationResult;
        ImportStatus = PhotoImportFolderStatus.ValidationFailed;
    }

    public SyncFolder? SyncFolder { get; }
    public PhotoImportFolderState? ImportFolder { get; private set; }

    public string Path { get; }
    public string Name { get; }
    public SyncFolderValidationResult ValidationResult { get; }

    public MappingSetupStatus Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public MappingErrorCode ErrorCode
    {
        get => _errorCode;
        private set => SetProperty(ref _errorCode, value);
    }

    public MappingErrorRenderingMode RenderingMode => MappingErrorRenderingMode.IconAndText;

    public PhotoImportFolderStatus ImportStatus
    {
        get => _importStatus;
        set => SetProperty(ref _importStatus, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int? NumberOfFilesToImport
    {
        get => _numberOfFilesToImport;
        set => SetProperty(ref _numberOfFilesToImport, value);
    }

    public int NumberOfImportedFiles
    {
        get => _numberOfImportedFiles;
        set => SetProperty(ref _numberOfImportedFiles, value);
    }

    public bool ImportIsCompleted
    {
        get => _importIsCompleted;
        private set => SetProperty(ref _importIsCompleted, value);
    }

    public bool NoPhotosFound
    {
        get => _noPhotosFound;
        private set => SetProperty(ref _noPhotosFound, value);
    }

    public void Update(PhotoImportFolderState photoImportFolder)
    {
        Ensure.IsTrue(photoImportFolder.MappingId == SyncFolder?.MappingId, "Folder mapping does not match", nameof(photoImportFolder));

        ImportFolder = photoImportFolder;
        Update();
    }

    public void Update()
    {
        Status = SyncFolder?.Status ?? MappingSetupStatus.None;
        ErrorCode = SyncFolder?.ErrorCode ?? MappingErrorCode.None;

        NumberOfFilesToImport = ImportFolder?.NumberOfFilesToImport;
        NumberOfImportedFiles = ImportFolder?.NumberOfImportedFiles ?? 0;

        ImportIsCompleted = ImportStatus is PhotoImportFolderStatus.Succeeded or PhotoImportFolderStatus.Failed;

        NoPhotosFound = ImportStatus is PhotoImportFolderStatus.Succeeded && NumberOfFilesToImport == 0 && NumberOfImportedFiles == 0;

        if (ImportIsCompleted)
        {
            ImportStatus = ImportFolder?.Status ?? PhotoImportFolderStatus.NotStarted;
        }
        else
        {
            ImportStatus = Status is MappingSetupStatus.Failed
                ? PhotoImportFolderStatus.SetupFailed
                : (ImportFolder?.Status ?? PhotoImportFolderStatus.NotStarted);
        }
    }
}
