using ProtonDrive.Client.Instrumentation.Observability;
using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.App.Instrumentation.Observability.TransferPerformance;

public sealed record DownloadSpeedMetric : ObservabilityMetric
{
    public DownloadSpeedMetric(ObservabilityMetricProperties properties)
        : base("drive_download_speed_histogram", Version: 1, DateTime.UtcNow.ToUnixTimeSeconds(), properties)
    {
    }
}
