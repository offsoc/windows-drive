using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ProtonDrive.Client.Instrumentation.Observability;

namespace ProtonDrive.App.Instrumentation.Observability;

internal sealed class GenericFileTransferMetricsFactory
{
    private readonly AttemptRetryMonitors _attemptRetryMonitors;

    public GenericFileTransferMetricsFactory(AttemptRetryMonitors attemptRetryMonitors)
    {
        _attemptRetryMonitors = attemptRetryMonitors;
    }

    public ImmutableList<ObservabilityMetric> GetFileUploadMetrics()
    {
        var shareTypes = Enum.GetValues(typeof(AttemptRetryShareType));

        var uploadMetrics = new List<ObservabilityMetric>(capacity: shareTypes.Length * 4);

        foreach (AttemptRetryShareType shareType in shareTypes)
        {
            var counters = _attemptRetryMonitors.UploadAttemptRetryMonitor[shareType].GetAndResetCounters();

            if (counters.FirstAttemptSuccesses > 0)
            {
                uploadMetrics.Add(GetUploadMetric(counters.FirstAttemptSuccesses, isSuccess: true, isRetry: false, shareType));
            }

            if (counters.FirstAttemptFailures > 0)
            {
                uploadMetrics.Add(GetUploadMetric(counters.FirstAttemptFailures, isSuccess: false, isRetry: false, shareType));
            }

            if (counters.RetrySuccesses > 0)
            {
                uploadMetrics.Add(GetUploadMetric(counters.RetrySuccesses, isSuccess: true, isRetry: true, shareType));
            }

            if (counters.RetryFailures > 0)
            {
                uploadMetrics.Add(GetUploadMetric(counters.RetryFailures, isSuccess: false, isRetry: true, shareType));
            }
        }

        return uploadMetrics.ToImmutableList();
    }

    public ImmutableList<ObservabilityMetric> GetFileDownloadMetrics()
    {
        var shareTypes = Enum.GetValues(typeof(AttemptRetryShareType));

        var downloadsMetrics = new List<ObservabilityMetric>(capacity: shareTypes.Length * 4);

        foreach (AttemptRetryShareType shareType in shareTypes)
        {
            var counters = _attemptRetryMonitors.DownloadAttemptRetryMonitor[shareType].GetAndResetCounters();

            if (counters.FirstAttemptSuccesses > 0)
            {
                downloadsMetrics.Add(GetDownloadMetric(counters.FirstAttemptSuccesses, isSuccess: true, isRetry: false, shareType));
            }

            if (counters.FirstAttemptFailures > 0)
            {
                downloadsMetrics.Add(GetDownloadMetric(counters.FirstAttemptFailures, isSuccess: false, isRetry: false, shareType));
            }

            if (counters.RetrySuccesses > 0)
            {
                downloadsMetrics.Add(GetDownloadMetric(counters.RetrySuccesses, isSuccess: true, isRetry: true, shareType));
            }

            if (counters.RetryFailures > 0)
            {
                downloadsMetrics.Add(GetDownloadMetric(counters.RetryFailures, isSuccess: false, isRetry: true, shareType));
            }
        }

        return downloadsMetrics.ToImmutableList();
    }

    private static UploadSuccessRateMetric GetUploadMetric(int counter, bool isSuccess, bool isRetry, AttemptRetryShareType shareType)
    {
        var shareTypeLabel = shareType switch
        {
            AttemptRetryShareType.Main => "main",
            AttemptRetryShareType.Standard => "shared",
            AttemptRetryShareType.Device => "device",
            AttemptRetryShareType.Photo => "photo",
            _ => throw new ArgumentOutOfRangeException(nameof(shareType), shareType, null),
        };

        var labels = new Dictionary<string, string>
        {
            { "status", isSuccess ? "success" : "failure" },
            { "retry", isRetry ? "true" : "false" },
            { "shareType", shareTypeLabel },
            { "initiator", "background" },
        };

        var properties = new ObservabilityMetricProperties(Value: counter, labels);
        return new UploadSuccessRateMetric(properties);
    }

    private static DownloadSuccessRateMetric GetDownloadMetric(int counter, bool isSuccess, bool isRetry, AttemptRetryShareType shareType)
    {
        var shareTypeLabel = shareType switch
        {
            AttemptRetryShareType.Main => "main",
            AttemptRetryShareType.Standard => "shared",
            AttemptRetryShareType.Device => "device",
            _ => throw new ArgumentOutOfRangeException(nameof(shareType), shareType, null),
        };

        var labels = new Dictionary<string, string>
        {
            { "status", isSuccess ? "success" : "failure" },
            { "retry", isRetry ? "true" : "false" },
            { "shareType", shareTypeLabel },
        };

        var properties = new ObservabilityMetricProperties(Value: counter, labels);
        return new DownloadSuccessRateMetric(properties);
    }
}
