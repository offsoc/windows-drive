using ProtonDrive.Client.Sdk.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Shared;

internal sealed record FailureTags(string VolumeType, string Type)
{
    public static FailureTags? TryParse(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 2 &&
            tags[0].Key == SdkMetrics.VolumeTypeKeyName && tags[0].Value is string volumeType &&
            tags[1].Key == SdkMetrics.FailureTypeKeyName && tags[1].Value is string type)
        {
            return new FailureTags(volumeType, type);
        }

        return null;
    }
}
