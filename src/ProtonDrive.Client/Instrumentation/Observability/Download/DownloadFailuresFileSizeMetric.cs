using System.Collections.Immutable;
using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.Client.Instrumentation.Observability.Download;

public sealed record DownloadFailuresFileSizeMetric : ObservabilityMetric
{
    public DownloadFailuresFileSizeMetric(long value)
        : base(
            "drive_sdk_download_errors_file_size_histogram",
            Version: 1,
            DateTime.UtcNow.ToUnixTimeSeconds(),
            new ObservabilityMetricProperties(value, ImmutableDictionary<string, string>.Empty))
    {
    }
}
