using ProtonDrive.Client.Instrumentation.Observability.Shared;

namespace ProtonDrive.Client.Instrumentation.Observability.Upload;

internal sealed class UploadMetricsSnapshot
{
    public required IReadOnlyDictionary<AttemptTags, int> Attempts { get; init; }
    public required IReadOnlyDictionary<FailureTags, int> Failures { get; init; }
    public required IReadOnlyCollection<long> FailuresFileSize { get; init; }
    public required IReadOnlyCollection<long> FailuresTransferSize { get; init; }
}
