using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ProtonDrive.App.Authentication;
using ProtonDrive.App.Onboarding;
using ProtonDrive.App.Windows.Views.Shared;
using ProtonDrive.Shared.Threading;

namespace ProtonDrive.App.Windows.Views.Onboarding;

internal sealed class OnboardingViewModel : ObservableObject, IOnboardingStateAware, ICloseable
{
    private readonly IStatefulSessionService _sessionService;
    private readonly IScheduler _scheduler;
    private readonly Dictionary<OnboardingStep, OnboardingStepViewModel> _onboardingStepViewModelMap;

    private OnboardingStepViewModel? _currentStep;

    public OnboardingViewModel(
        SyncFolderSelectionStepViewModel syncFolderSelectionStepViewModel,
        AccountRootFolderSelectionStepViewModel accountRootFolderSelectionStep,
        UpgradeStorageStepViewModel upgradeStorageStepViewModel,
        IStatefulSessionService sessionService,
        [FromKeyedServices("Dispatcher")] IScheduler scheduler)
    {
        _sessionService = sessionService;
        _scheduler = scheduler;

        _onboardingStepViewModelMap = new Dictionary<OnboardingStep, OnboardingStepViewModel>
        {
            { OnboardingStep.SyncFolderSelection, syncFolderSelectionStepViewModel },
            { OnboardingStep.AccountRootFolderSelection, accountRootFolderSelectionStep },
            { OnboardingStep.UpgradeStorage, upgradeStorageStepViewModel },
        };
    }

    public OnboardingStepViewModel? CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    void IOnboardingStateAware.OnboardingStateChanged(OnboardingState value)
    {
        Schedule(() => UpdateCurrentStep(value));
    }

    void ICloseable.Close()
    {
        switch (CurrentStep)
        {
            case UpgradeStorageStepViewModel { IsActive: true } upgradeStorageStepViewModel:
                upgradeStorageStepViewModel.CompleteStep();
                break;

            case { IsActive: true }:
                // Onboarding is not completed, signing out
                _sessionService.EndSessionAsync();
                break;
        }
    }

    private void UpdateCurrentStep(OnboardingState state)
    {
        var previousStep = CurrentStep;
        var currentStep = GetStepViewModel(state);

        if (previousStep == currentStep)
        {
            if (state.Step is OnboardingStep.None)
            {
                previousStep?.Deactivate();
            }

            return;
        }

        previousStep?.Deactivate();
        currentStep?.Activate();

        CurrentStep = currentStep;
    }

    private OnboardingStepViewModel? GetStepViewModel(OnboardingState state)
    {
        return state.Step is OnboardingStep.None
            ? CurrentStep
            : _onboardingStepViewModelMap[state.Step];
    }

    private void Schedule(Action action)
    {
        _scheduler.Schedule(action);
    }
}
