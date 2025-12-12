using ProtonDrive.App.Account;
using ProtonDrive.App.Sync;
using ProtonDrive.Shared.Features;
using ProtonDrive.Sync.Shared.SyncActivity;

namespace ProtonDrive.App.Instrumentation.Observability.TransferPerformance;

internal sealed class TransferPerformanceMeter : ISyncActivityAware, IAccountSwitchingAware, IFeatureFlagsAware
{
    private readonly TransferPerformanceMonitors _monitors;

    private TransferPipeline _downloadPipeline = TransferPipeline.Default;
    private TransferPipeline _uploadPipeline = TransferPipeline.Default;

    public TransferPerformanceMeter(TransferPerformanceMonitors monitors)
    {
        _monitors = monitors;
    }

    void ISyncActivityAware.OnSyncActivityChanged(SyncActivityItem<long> item)
    {
        if (item.Source is not SyncActivitySource.OperationExecution and not SyncActivitySource.OnDemandFileHydration)
        {
            return;
        }

        if (item.ActivityType is not SyncActivityType.Upload and not SyncActivityType.Download)
        {
            return;
        }

        var transferPipeline = item.ActivityType is SyncActivityType.Upload ? _uploadPipeline : _downloadPipeline;
        var key = (item.ActivityType, TransferContext.Background, transferPipeline);
        var monitor = _monitors[key];

        monitor.HandleProgress(item);
    }

    void IAccountSwitchingAware.OnAccountSwitched()
    {
        // We clear data upon account switching, because same ID values would be reused for different files
        _monitors.Clear();
    }

    void IFeatureFlagsAware.OnFeatureFlagsChanged(IReadOnlyDictionary<Feature, bool> features)
    {
        _downloadPipeline = features[Feature.DriveWindowsSdkDownloadMain] ? TransferPipeline.Default : TransferPipeline.Legacy;
        _uploadPipeline = features[Feature.DriveWindowsSdkUploadMain] ? TransferPipeline.Default : TransferPipeline.Legacy;
    }
}
