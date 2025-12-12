using System.Diagnostics.Metrics;
using Proton.Drive.Sdk.Telemetry;

namespace ProtonDrive.Client.Sdk.Metrics;

internal sealed class IntegrityMetrics
{
    public const string MeterName = "Proton.Drive.SDK.GenericIntegrity";
    public const string DecryptionErrorsMetricName = "proton.drive.sdk.generic.integrity.decryption_errors";
    public const string VerificationErrorsMetricName = "proton.drive.sdk.generic.integrity.verification_errors";
    public const string BlockVerificationErrorsMetricName = "proton.drive.sdk.generic.integrity.block_verification_errors";

    public const string FieldKeyName = "field";
    public const string FromBefore2024KeyName = "fromBefore2024";
    public const string AddressMatchingDefaultShareKeyName = "addressMatchingDefaultShare";
    public const string RetryHelpedKeyName = "retryHelped";

    private static readonly Dictionary<EncryptedField, string> FieldMapping = new()
    {
        { EncryptedField.ShareKey, "shareKey" },
        { EncryptedField.NodeKey, "nodeKey" },
        { EncryptedField.NodeName, "nodeName" },
        { EncryptedField.NodeHashKey, "nodeHashKey" },
        { EncryptedField.NodeExtendedAttributes, "nodeExtendedAttributes" },
        { EncryptedField.NodeContentKey, "nodeContentKey" },
        { EncryptedField.Content, "content" },
    };

    private readonly Counter<int> _decryptionErrors;
    private readonly Counter<int> _verificationErrors;
    private readonly Counter<int> _blockVerificationErrors;

    public IntegrityMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _decryptionErrors = meter.CreateCounter<int>(
            name: DecryptionErrorsMetricName,
            unit: "{number}",
            description: "Count of decryption errors");

        _verificationErrors = meter.CreateCounter<int>(
            name: VerificationErrorsMetricName,
            unit: "{number}",
            description: "Count of verification errors");

        _blockVerificationErrors = meter.CreateCounter<int>(
            name: BlockVerificationErrorsMetricName,
            unit: "{number}",
            description: "Count of block verification errors");
    }

    public void Record(DecryptionErrorEvent decryptionErrorEvent)
    {
        _decryptionErrors.Add(
            1,
            GetTag(SdkMetrics.VolumeTypeKeyName, MapVolumeType(decryptionErrorEvent.VolumeType)),
            GetTag(FieldKeyName, MapField(decryptionErrorEvent.Field)),
            GetTag(FromBefore2024KeyName, MapBoolean(decryptionErrorEvent.FromBefore2024)));
    }

    public void Record(VerificationErrorEvent verificationErrorEvent)
    {
        _verificationErrors.Add(
            1,
            GetTag(SdkMetrics.VolumeTypeKeyName, MapVolumeType(verificationErrorEvent.VolumeType)),
            GetTag(FieldKeyName, MapField(verificationErrorEvent.Field)),
            GetTag(AddressMatchingDefaultShareKeyName, MapBoolean(verificationErrorEvent.AddressMatchingDefaultShare)),
            GetTag(FromBefore2024KeyName, MapBoolean(verificationErrorEvent.FromBefore2024)));
    }

    public void Record(BlockVerificationErrorEvent blockVerificationErrorEvent)
    {
        _blockVerificationErrors.Add(
            1,
            GetTag(RetryHelpedKeyName, MapBoolean(blockVerificationErrorEvent.RetryHelped)));
    }

    private static string MapVolumeType(VolumeType volumeType)
    {
        return VolumeTypeMapping.GetValueOrDefault(volumeType);
    }

    private static string MapField(EncryptedField field)
    {
        return FieldMapping.GetValueOrDefault(field, "unknown");
    }

    private static string MapBoolean(bool? value)
    {
        return value switch
        {
            true => "yes",
            false => "no",
            null => "unknown",
        };
    }

    private static KeyValuePair<string, object?> GetTag(string key, string value)
    {
        return new KeyValuePair<string, object?>(key, value);
    }
}
