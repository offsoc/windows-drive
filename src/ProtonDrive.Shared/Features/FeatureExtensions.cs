namespace ProtonDrive.Shared.Features;

public static class FeatureExtensions
{
    public static bool IsEnabled(this IReadOnlyCollection<(Feature Feature, bool IsEnabled)> features, Feature feature)
    {
        return features.Any(x => x.Feature == feature && x.IsEnabled);
    }
}
