using System.Collections.Immutable;

namespace ProtonDrive.Client.Instrumentation.Observability;

public interface IMetricsService
{
    void Start();
    void Stop();

    ImmutableList<ObservabilityMetric> GetMetrics(bool userHasAPaidPlan);
}
