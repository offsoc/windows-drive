using ProtonDrive.Client.Sdk.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Shared;

internal sealed record AttemptTags(string VolumeType, string Status)
{
    public static AttemptTags? TryParse(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 2 &&
            tags[0].Key == SdkMetrics.VolumeTypeKeyName && tags[0].Value is string volumeType &&
            tags[1].Key == SdkMetrics.AttemptStatusKeyName && tags[1].Value is string status)
        {
            return new AttemptTags(volumeType, status);
        }

        return null;
    }
}
