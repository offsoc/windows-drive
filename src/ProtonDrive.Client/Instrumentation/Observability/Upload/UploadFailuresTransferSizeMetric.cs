using System.Collections.Immutable;
using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.Client.Instrumentation.Observability.Upload;

public sealed record UploadFailuresTransferSizeMetric : ObservabilityMetric
{
    public UploadFailuresTransferSizeMetric(long value)
        : base(
            "drive_sdk_upload_errors_transfer_size_histogram",
            Version: 1,
            DateTime.UtcNow.ToUnixTimeSeconds(),
            new ObservabilityMetricProperties(value, ImmutableDictionary<string, string>.Empty))
    {
    }
}
