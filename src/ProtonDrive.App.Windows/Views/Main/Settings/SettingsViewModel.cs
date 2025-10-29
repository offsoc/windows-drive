using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ProtonDrive.App.Localization;
using ProtonDrive.App.Windows.SystemIntegration;

namespace ProtonDrive.App.Windows.Views.Main.Settings;

internal class SettingsViewModel : PageViewModel
{
    private readonly IOperatingSystemIntegrationService _operatingSystemIntegrationService;
    private readonly ILanguageService _languageService;

    private bool _appIsOpeningOnStartup;
    private bool _languageHasChanged;
    private Language _selectedLanguage;

    public SettingsViewModel(
        IApp app,
        IOperatingSystemIntegrationService operatingSystemIntegrationService,
        AccountRootSyncFolderViewModel accountRootSyncFolder,
        ILanguageService languageService)
    {
        AccountRootSyncFolder = accountRootSyncFolder;
        _operatingSystemIntegrationService = operatingSystemIntegrationService;
        _languageService = languageService;
        _appIsOpeningOnStartup = _operatingSystemIntegrationService.GetRunApplicationOnStartup();

        SupportedLanguages = languageService.GetSupportedLanguages().ToList();
        _selectedLanguage = languageService.CurrentLanguage;

        RestartAppCommand = new AsyncRelayCommand(app.RestartAsync);
    }

    public IReadOnlyList<Language> SupportedLanguages { get; }

    public Language SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                OnSelectedLanguageChanged(value);
            }
        }
    }

    public bool LanguageHasChanged
    {
        get => _languageHasChanged;
        private set => SetProperty(ref _languageHasChanged, value);
    }

    public bool AppIsOpeningOnStartup
    {
        get => _appIsOpeningOnStartup;

        set
        {
            if (SetProperty(ref _appIsOpeningOnStartup, value))
            {
                _operatingSystemIntegrationService.SetRunApplicationOnStartup(value);
            }
        }
    }

    public ICommand RestartAppCommand { get; }

    public AccountRootSyncFolderViewModel AccountRootSyncFolder { get; }

    internal override void OnActivated()
    {
        AccountRootSyncFolder.ClearValidationResult();
    }

    private void OnSelectedLanguageChanged(Language value)
    {
        _languageService.CurrentLanguage = value;
        LanguageHasChanged = _languageService.HasLanguageChanged;
    }
}
