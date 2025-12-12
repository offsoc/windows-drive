using ProtonDrive.Client.Sdk.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Integrity;

internal sealed record VerificationFailureTags(string VolumeType, string Field, string AddressMatchingDefaultShare, string FromBefore2024)
{
    public static VerificationFailureTags? TryParse(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 4 &&
            tags[0].Key == SdkMetrics.VolumeTypeKeyName && tags[0].Value is string volumeType &&
            tags[1].Key == IntegrityMetrics.FieldKeyName && tags[1].Value is string field &&
            tags[2].Key == IntegrityMetrics.AddressMatchingDefaultShareKeyName && tags[2].Value is string addressMatchingDefaultShare &&
            tags[3].Key == IntegrityMetrics.FromBefore2024KeyName && tags[3].Value is string fromBefore2024)
        {
            return new VerificationFailureTags(volumeType, field, addressMatchingDefaultShare, fromBefore2024);
        }

        return null;
    }
}
