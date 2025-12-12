using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.Client.Instrumentation.Observability.Download;

public sealed record DownloadFailuresMetric : ObservabilityMetric
{
    public DownloadFailuresMetric(long value, IReadOnlyDictionary<string, string> labels)
        : base(
            "drive_sdk_download_errors_total",
            Version: 1,
            DateTime.UtcNow.ToUnixTimeSeconds(),
            new ObservabilityMetricProperties(value, labels))
    {
    }
}
