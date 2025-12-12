using Proton.Drive.Sdk.Telemetry;
using Proton.Sdk.Telemetry;

namespace ProtonDrive.Client.Sdk.Metrics;

internal sealed class SdkMetrics(UploadMetrics uploadMetrics, DownloadMetrics downloadMetrics, IntegrityMetrics integrityMetrics)
{
    public const string VolumeTypeKeyName = "volumeType";
    public const string AttemptStatusKeyName = "status";
    public const string FailureTypeKeyName = "type";
    public const string UserPlanKeyName = "userPlan";

    public void Record(IMetricEvent metricEvent)
    {
        switch (metricEvent)
        {
            case UploadEvent uploadEvent:
                uploadMetrics.Record(uploadEvent);
                break;

            case DownloadEvent downloadEvent:
                downloadMetrics.Record(downloadEvent);
                break;

            case DecryptionErrorEvent decryptionErrorEvent:
                integrityMetrics.Record(decryptionErrorEvent);
                break;

            case VerificationErrorEvent verificationErrorEvent:
                integrityMetrics.Record(verificationErrorEvent);
                break;

            case BlockVerificationErrorEvent blockVerificationErrorEvent:
                integrityMetrics.Record(blockVerificationErrorEvent);
                break;
        }
    }
}
