using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ProtonDrive.App.Account;
using ProtonDrive.App.Onboarding;
using ProtonDrive.App.Windows.Configuration.Hyperlinks;
using ProtonDrive.App.Windows.Extensions;
using ProtonDrive.App.Windows.Services;
using ProtonDrive.App.Windows.Toolkit.Threading;

namespace ProtonDrive.App.Windows.Views.Onboarding;

internal sealed class UpgradeStorageStepViewModel : OnboardingStepViewModel, IUserStateAware
{
    private readonly IOnboardingService _onboardingService;
    private readonly IExternalHyperlinks _externalHyperlinks;
    private readonly IUpgradeStoragePlanAvailabilityVerifier _upgradeStoragePlanAvailabilityVerifier;
    private readonly DispatcherScheduler _scheduler;
    private readonly IReadOnlyList<StorageUpgradeOffer> _allOffers;
    private readonly ObservableCollection<StorageUpgradeOffer> _relevantOffers = [];

    private UserState _userState = UserState.Empty;

    public UpgradeStorageStepViewModel(
        IOnboardingService onboardingService,
        IExternalHyperlinks externalHyperlinks,
        IUpgradeStoragePlanAvailabilityVerifier upgradeStoragePlanAvailabilityVerifier,
        DispatcherScheduler scheduler)
    {
        _onboardingService = onboardingService;
        _externalHyperlinks = externalHyperlinks;
        _upgradeStoragePlanAvailabilityVerifier = upgradeStoragePlanAvailabilityVerifier;
        _scheduler = scheduler;

        UpgradeCommand = new RelayCommand(OpenUpgradeStorageLinkAndContinue);
        SkipCommand = new RelayCommand(CompleteStep);

        _allOffers =
        [
            new StorageUpgradeOffer("Unlimited", StorageInGb: 500, NumberOfUsers: 1, IsRecommended: true, UpgradeCommand),
            new StorageUpgradeOffer("Family", StorageInGb: 3000, NumberOfUsers: 6, IsRecommended: false, UpgradeCommand),
        ];

        RelevantOffers = new ReadOnlyObservableCollection<StorageUpgradeOffer>(_relevantOffers);
    }

    public ReadOnlyObservableCollection<StorageUpgradeOffer> RelevantOffers { get; }

    public ICommand SkipCommand { get; }

    private ICommand UpgradeCommand { get; }

    void IUserStateAware.OnUserStateChanged(UserState value)
    {
        var previousState = _userState;
        _userState = value;

        if (previousState.MaxSpace != value.MaxSpace ||
            previousState.SubscriptionPlanCode != value.SubscriptionPlanCode)
        {
            Schedule(UpdateAvailableOffers);
        }
    }

    public override void Activate()
    {
        base.Activate();

        Schedule(UpdateAvailableOffers);
    }

    public void CompleteStep()
    {
        _onboardingService.CompleteStep(OnboardingStep.UpgradeStorage);
    }

    private void UpdateAvailableOffers()
    {
        if (!IsActive)
        {
            return;
        }

        var userState = _userState;

        if (!_upgradeStoragePlanAvailabilityVerifier.UpgradedPlanIsAvailable(UpgradeStoragePlanMode.Onboarding, userState.SubscriptionPlanCode))
        {
            CompleteStep();
            return;
        }

        var availableStorageSpaceInGb = userState.MaxSpace / 1_000_000_000;
        _relevantOffers.Clear();
        _relevantOffers.AddEach(_allOffers.Where(x => x.StorageInGb > availableStorageSpaceInGb));

        if (_relevantOffers.Count == 0)
        {
            CompleteStep();
        }
    }

    private void OpenUpgradeStorageLinkAndContinue()
    {
        _externalHyperlinks.UpgradePlanFromOnboarding.Open();
        CompleteStep();
    }

    private void Schedule(Action action)
    {
        _scheduler.Schedule(action);
    }
}
