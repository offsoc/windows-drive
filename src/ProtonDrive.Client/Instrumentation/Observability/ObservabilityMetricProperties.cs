namespace ProtonDrive.Client.Instrumentation.Observability;

public sealed record ObservabilityMetricProperties(int Value, IReadOnlyDictionary<string, string> Labels);
