using CommunityToolkit.Mvvm.ComponentModel;
using ProtonDrive.App.Mapping;
using ProtonDrive.App.Mapping.SyncFolders;
using ProtonDrive.App.Windows.Views.Shared;

namespace ProtonDrive.App.Windows.Views.Main.Photos;

internal sealed class ImportFolderViewModel : ObservableObject, IMappingStatusViewModel
{
    private int _numberOfProcessedItems;
    private string? _errorMessage;
    private MappingSetupStatus _status;
    private MappingErrorCode _errorCode = MappingErrorCode.None;

    public ImportFolderViewModel(string path, string name, SyncFolder syncFolder)
    {
        Path = path;
        Name = name;
        SyncFolder = syncFolder;
        Status = syncFolder.Status;
    }

    public ImportFolderViewModel(string path, string name, SyncFolderValidationResult validationResult)
    {
        Path = path;
        Name = name;
        ValidationResult = validationResult;

        if (NumberOfItemsToProcess == 0 && validationResult is SyncFolderValidationResult.Succeeded)
        {
            ErrorMessage = Resources.Strings.Main_Photos_Folders_NoItemsFound_Label;
        }

        Status = string.IsNullOrEmpty(ErrorMessage) && validationResult is SyncFolderValidationResult.Succeeded
            ? MappingSetupStatus.SettingUp
            : MappingSetupStatus.Failed;
    }

    public string Path { get; }
    public string Name { get; }
    public int NumberOfItemsToProcess { get; }
    public SyncFolder? SyncFolder { get; }
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

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int NumberOfProcessedItems
    {
        get => _numberOfProcessedItems;
        private set => SetProperty(ref _numberOfProcessedItems, value);
    }

    public void Update()
    {
        Status = SyncFolder?.Status ?? MappingSetupStatus.None;
        ErrorCode = SyncFolder?.ErrorCode ?? MappingErrorCode.None;
    }
}
