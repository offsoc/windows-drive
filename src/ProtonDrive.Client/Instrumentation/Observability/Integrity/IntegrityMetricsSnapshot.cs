namespace ProtonDrive.Client.Instrumentation.Observability.Integrity;

internal sealed class IntegrityMetricsSnapshot
{
    public required IReadOnlyDictionary<DecryptionFailureTags, int> DecryptionFailures { get; init; }
    public required IReadOnlyDictionary<VerificationFailureTags, int> VerificationFailures { get; init; }
    public required IReadOnlyDictionary<BlockVerificationFailureTags, int> BlockVerificationFailures { get; init; }
}
