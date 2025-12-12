using ProtonDrive.Client.Sdk.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Integrity;

internal sealed record BlockVerificationFailureTags(string RetryHelped)
{
    public static BlockVerificationFailureTags? TryParse(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 1 &&
            tags[0].Key == IntegrityMetrics.RetryHelpedKeyName && tags[0].Value is string retryHelped)
        {
            return new BlockVerificationFailureTags(retryHelped);
        }

        return null;
    }
}
