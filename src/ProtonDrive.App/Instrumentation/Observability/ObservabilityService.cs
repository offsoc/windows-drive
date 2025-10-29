using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Instrumentation.Observability.TransferPerformance;
using ProtonDrive.App.Settings.Remote;
using ProtonDrive.Client;
using ProtonDrive.Client.Instrumentation.Observability;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Configuration;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.Threading;

namespace ProtonDrive.App.Instrumentation.Observability;

internal sealed class ObservabilityService : IRemoteSettingsAware
{
    private readonly IClock _clock;
    private readonly IObservabilityApiClient _observabilityApiClient;
    private readonly GenericFileTransferMetricsFactory _genericFileTransferMetricsFactory;
    private readonly GenericTransferPerformanceMetricsFactory _genericTransferPerformanceMetricsFactory;
    private readonly ILogger<ObservabilityService> _logger;

    private readonly CancellationHandle _cancellationHandle = new();
    private readonly TimeSpan _transferPerformanceReportInterval;
    private readonly TimeSpan _period;

    private PeriodicTimer _timer;
    private Task? _timerTask;
    private TickCount _nextTransferPerformanceReportTime;

    public ObservabilityService(
        AppConfig appConfig,
        IClock clock,
        IObservabilityApiClient observabilityApiClient,
        GenericFileTransferMetricsFactory genericFileTransferMetricsFactory,
        GenericTransferPerformanceMetricsFactory genericTransferPerformanceMetricsFactory,
        ILogger<ObservabilityService> logger)
    {
        _clock = clock;
        _observabilityApiClient = observabilityApiClient;
        _genericFileTransferMetricsFactory = genericFileTransferMetricsFactory;
        _genericTransferPerformanceMetricsFactory = genericTransferPerformanceMetricsFactory;
        _logger = logger;

        _transferPerformanceReportInterval = appConfig.PeriodicTransferPerformanceReportInterval;
        _period = appConfig.PeriodicObservabilityReportInterval.RandomizedWithDeviation(0.2);
        _timer = new PeriodicTimer(_period);
    }

    void IRemoteSettingsAware.OnRemoteSettingsChanged(RemoteSettings settings)
    {
        if (settings.IsTelemetryEnabled)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    private void Start()
    {
        if (_timerTask is not null)
        {
            return;
        }

        Clear();

        _nextTransferPerformanceReportTime = _clock.TickCount + _transferPerformanceReportInterval;
        _timer = new PeriodicTimer(_period);
        _timerTask = PeriodicallySendMetricsAsync(_cancellationHandle.Token);
    }

    private void Stop()
    {
        if (_timerTask is null)
        {
            return;
        }

        _cancellationHandle.Cancel();
        _timerTask = null;
        _timer.Dispose();
    }

    private async Task PeriodicallySendMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await SendMetricsAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            /* Do nothing */
        }
    }

    private async Task SendMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var metrics = GetMetrics();

            if (metrics.Metrics.Count > 0)
            {
                await _observabilityApiClient.SendMetricsAsync(metrics, cancellationToken).ThrowOnFailure().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to send observability metrics: {ErrorCode} : {ErrorMessage}", ex.GetRelevantFormattedErrorCode(), ex.Message);
        }
    }

    private ObservabilityMetrics GetMetrics()
    {
        var uploadMetrics = _genericFileTransferMetricsFactory.GetFileUploadMetrics();
        var downloadMetrics = _genericFileTransferMetricsFactory.GetFileDownloadMetrics();
        var performanceMetrics = GetTransferPerformanceMetrics();

        var metrics = Enumerable.Empty<ObservabilityMetric>()
            .Concat(uploadMetrics)
            .Concat(downloadMetrics)
            .Concat(performanceMetrics)
            .ToList();

        return new ObservabilityMetrics(metrics);
    }

    private void Clear()
    {
        _genericTransferPerformanceMetricsFactory.Clear();
    }

    private ImmutableList<ObservabilityMetric> GetTransferPerformanceMetrics()
    {
        var now = _clock.TickCount;

        // Transfer performance is reported less often than other metrics
        if (now < _nextTransferPerformanceReportTime)
        {
            return [];
        }

        _nextTransferPerformanceReportTime = now + _transferPerformanceReportInterval;

        return _genericTransferPerformanceMetricsFactory.GetMetrics();
    }
}
