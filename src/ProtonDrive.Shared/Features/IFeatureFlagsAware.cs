namespace ProtonDrive.Shared.Features;

/// <summary>
/// Interface for components that need to be notified when feature flags change.
/// </summary>
public interface IFeatureFlagsAware
{
    /// <summary>
    /// Called when feature flags have changed.
    /// </summary>
    /// <param name="features">The collection of features and their current enabled state.</param>
    void OnFeatureFlagsChanged(IReadOnlyCollection<(Feature Feature, bool IsEnabled)> features);
}
