using System;
using ProtonDrive.Client.Instrumentation.Observability;
using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.App.Instrumentation.Observability.TransferPerformance;

public sealed record UploadSpeedMetric : ObservabilityMetric
{
    public UploadSpeedMetric(ObservabilityMetricProperties properties)
        : base("drive_upload_speed_histogram", Version: 1, DateTime.UtcNow.ToUnixTimeSeconds(), properties)
    {
    }
}
