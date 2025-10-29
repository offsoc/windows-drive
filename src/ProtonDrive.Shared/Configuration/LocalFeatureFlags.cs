namespace ProtonDrive.Shared.Configuration;

public sealed class LocalFeatureFlags
{
    public bool UpgradeStorageOnboardingStepEnabled { get; internal set; }

    public bool DriveSdkEnabled { get; internal set; }

    // Example of feature flag property:
    // public bool SomeFeatureEnabled { get; internal set; }
}
