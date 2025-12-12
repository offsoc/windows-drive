using System.Diagnostics.Metrics;
using ProtonDrive.Client.Sdk.Metrics;
using ProtonDrive.Shared.Diagnostics.Metrics;

namespace ProtonDrive.Client.Instrumentation.Observability.Integrity;

internal sealed class IntegrityMetricsCollector
{
    private readonly AggregatingCollector<int, DecryptionFailureTags> _decryptionFailures = new();
    private readonly AggregatingCollector<int, VerificationFailureTags> _verificationFailures = new();
    private readonly AggregatingCollector<int, BlockVerificationFailureTags> _blockVerificationFailures = new();

    private MeterListener? _meterListener;
    private Instrument? _decryptionFailuresInstrument;
    private Instrument? _verificationFailuresInstrument;
    private Instrument? _blockVerificationFailuresInstrument;

    public IntegrityMetricsSnapshot GetMeasurementSnapshot()
    {
        _meterListener?.RecordObservableInstruments();

        return new IntegrityMetricsSnapshot
        {
            DecryptionFailures = _decryptionFailures.GetMeasurementSnapshot(),
            VerificationFailures = _verificationFailures.GetMeasurementSnapshot(),
            BlockVerificationFailures = _blockVerificationFailures.GetMeasurementSnapshot(),
        };
    }

    public void Start()
    {
        _decryptionFailures.Clear();
        _verificationFailures.Clear();
        _blockVerificationFailures.Clear();

        _meterListener = new MeterListener();
        _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);

        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument is { Meter.Name: IntegrityMetrics.MeterName, Name: IntegrityMetrics.DecryptionErrorsMetricName })
            {
                _decryptionFailuresInstrument = instrument;
                listener.EnableMeasurementEvents(instrument);
            }

            if (instrument is { Meter.Name: IntegrityMetrics.MeterName, Name: IntegrityMetrics.VerificationErrorsMetricName })
            {
                _verificationFailuresInstrument = instrument;
                listener.EnableMeasurementEvents(instrument);
            }

            if (instrument is { Meter.Name: IntegrityMetrics.MeterName, Name: IntegrityMetrics.BlockVerificationErrorsMetricName })
            {
                _blockVerificationFailuresInstrument = instrument;
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.Start();
    }

    public void Stop()
    {
        _meterListener?.Dispose();
        _meterListener = null;
    }

    private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        if (instrument == _decryptionFailuresInstrument && DecryptionFailureTags.TryParse(tags) is { } decryptionFailuresKey)
        {
            _decryptionFailures.RecordMeasurement(decryptionFailuresKey, measurement);
        }

        if (instrument == _verificationFailuresInstrument && VerificationFailureTags.TryParse(tags) is { } verificationFailuresKey)
        {
            _verificationFailures.RecordMeasurement(verificationFailuresKey, measurement);
        }

        if (instrument == _blockVerificationFailuresInstrument && BlockVerificationFailureTags.TryParse(tags) is { } blockVerificationFailuresKey)
        {
            _blockVerificationFailures.RecordMeasurement(blockVerificationFailuresKey, measurement);
        }
    }
}
