using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ProtonDrive.App.Account;
using ProtonDrive.App.Mapping;
using ProtonDrive.App.Mapping.SyncFolders;
using ProtonDrive.App.Windows.Configuration.Hyperlinks;
using ProtonDrive.App.Windows.Extensions;
using ProtonDrive.Shared.Threading;

namespace ProtonDrive.App.Windows.Views.Main.Photos;

internal sealed class PhotosImportViewModel : ObservableObject, ISyncFoldersAware, IAccountSwitchingAware
{
    private readonly IPhotoFolderService _photoFolderService;
    private readonly IExternalHyperlinks _externalHyperlinks;
    private readonly IScheduler _scheduler;

    private bool _isDisplayingImportGooglePhotosDetails;
    private string? _lastSelectedParentFolderPath;

    public PhotosImportViewModel(
        IPhotoFolderService photoFolderService,
        IExternalHyperlinks externalHyperlinks,
        [FromKeyedServices("Dispatcher")] IScheduler scheduler)
    {
        _photoFolderService = photoFolderService;
        _externalHyperlinks = externalHyperlinks;
        _scheduler = scheduler;

        DisplayImportGooglePhotosDetailsCommand = new RelayCommand(() => IsDisplayingImportGooglePhotosDetails = true);
        OpenImportGooglePhotosSupportUrlCommand = new RelayCommand(OpenImportPhotosSupportUrl);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync);
        RemoveFolderCommand = new AsyncRelayCommand<ImportFolderViewModel?>(RemoveFolderAsync);
    }

    public bool IsDisplayingImportGooglePhotosDetails
    {
        get => _isDisplayingImportGooglePhotosDetails;
        set => SetProperty(ref _isDisplayingImportGooglePhotosDetails, value);
    }

    public ICommand OpenImportGooglePhotosSupportUrlCommand { get; }

    public ICommand AddFolderCommand { get; }

    public ICommand DisplayImportGooglePhotosDetailsCommand { get; }

    public ICommand RemoveFolderCommand { get; }

    public ObservableCollection<ImportFolderViewModel> Folders { get; } = [];

    void ISyncFoldersAware.OnSyncFolderChanged(SyncFolderChangeType changeType, SyncFolder folder)
    {
        if (folder.Type is not SyncFolderType.PhotoImport)
        {
            return;
        }

        Schedule(HandleImportFolderChange);

        return;

        void HandleImportFolderChange()
        {
            switch (changeType)
            {
                case SyncFolderChangeType.Added:
                    var importFolder = new ImportFolderViewModel(folder.LocalPath, Path.GetFileName(folder.LocalPath), folder);
                    Folders.Add(importFolder);
                    break;

                case SyncFolderChangeType.Updated:
                    var item = Folders.FirstOrDefault(x => x.SyncFolder?.Equals(folder) ?? false);
                    item?.Update();
                    break;

                case SyncFolderChangeType.Removed:
                    Folders.RemoveFirst(x => x.SyncFolder?.Equals(folder) ?? false);
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(changeType), (int)changeType, typeof(SyncFolderChangeType));
            }
        }
    }

    void IAccountSwitchingAware.OnAccountSwitched()
    {
        Schedule(OnAccountSwitched);
    }

    private void OpenImportPhotosSupportUrl()
    {
        _externalHyperlinks.ImportPhotosSupport.Open();
    }

    private async Task AddFolderAsync(CancellationToken cancellationToken)
    {
        var folderPickingDialog = new OpenFolderDialog
        {
            InitialDirectory = _lastSelectedParentFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };

        var result = folderPickingDialog.ShowDialog();

        if (result is not true)
        {
            return;
        }

        var folderPath = folderPickingDialog.FolderName;
        _lastSelectedParentFolderPath = Path.GetDirectoryName(folderPath);

        if (Folders.Any(x => x.Path.Equals(folderPath)))
        {
            return;
        }

        var validationResult = _photoFolderService.ValidateFolder(folderPath);

        if (validationResult is SyncFolderValidationResult.Succeeded)
        {
            await _photoFolderService.AddImportFolderAsync(folderPath, cancellationToken).ConfigureAwait(true);
        }
        else
        {
            var importFolder = new ImportFolderViewModel(folderPath, Path.GetFileName(folderPath), validationResult);

            Folders.Add(importFolder);
        }

        IsDisplayingImportGooglePhotosDetails = false;
    }

    private async Task RemoveFolderAsync(ImportFolderViewModel? folder, CancellationToken cancellationToken)
    {
        if (folder is null)
        {
            return;
        }

        if (folder.SyncFolder is not null)
        {
            await _photoFolderService.RemoveFolderAsync(folder.SyncFolder, cancellationToken).ConfigureAwait(true);
        }
        else
        {
            Folders.Remove(folder);
        }
    }

    private void OnAccountSwitched()
    {
        foreach (var folder in Folders.Where(x => x.SyncFolder is null).ToList())
        {
            Folders.Remove(folder);
        }

        IsDisplayingImportGooglePhotosDetails = false;
    }

    private void Schedule(Action action)
    {
        _scheduler.Schedule(action);
    }
}
