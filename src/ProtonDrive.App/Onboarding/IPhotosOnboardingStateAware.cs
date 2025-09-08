namespace ProtonDrive.App.Onboarding;

public interface IPhotosOnboardingStateAware
{
    void OnPhotosOnboardingStateChanged(OnboardingStatus value);
}
