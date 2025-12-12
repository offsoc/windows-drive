using ProtonDrive.Client.Sdk.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Integrity;

internal sealed record DecryptionFailureTags(string VolumeType, string Field, string FromBefore2024)
{
    public static DecryptionFailureTags? TryParse(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 3 &&
            tags[0].Key == SdkMetrics.VolumeTypeKeyName && tags[0].Value is string volumeType &&
            tags[1].Key == IntegrityMetrics.FieldKeyName && tags[1].Value is string field &&
            tags[2].Key == IntegrityMetrics.FromBefore2024KeyName && tags[2].Value is string fromBefore2024)
        {
            return new DecryptionFailureTags(volumeType, field, fromBefore2024);
        }

        return null;
    }
}
