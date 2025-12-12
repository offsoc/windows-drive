namespace ProtonDrive.Client.Instrumentation.Observability;

public sealed record ObservabilityMetricProperties(long Value, IReadOnlyDictionary<string, string> Labels);
