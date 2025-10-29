using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Instrumentation.Telemetry;

public sealed record TelemetryEvent(
    string MeasurementGroup,
    [property: JsonPropertyName("Event")] string EventName,
    IReadOnlyDictionary<string, double> Values,
    IReadOnlyDictionary<string, string> Dimensions);
