using ProtonDrive.Client.Sdk.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Shared;

internal sealed record FailuresImpactedUserTags(string VolumeType, string UserPlan)
{
    public static FailuresImpactedUserTags? TryParse(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 2 &&
            tags[0].Key == SdkMetrics.VolumeTypeKeyName && tags[0].Value is string volumeType &&
            tags[1].Key == SdkMetrics.UserPlanKeyName && tags[1].Value is string userPlan)
        {
            return new FailuresImpactedUserTags(volumeType, userPlan);
        }

        return null;
    }
}
