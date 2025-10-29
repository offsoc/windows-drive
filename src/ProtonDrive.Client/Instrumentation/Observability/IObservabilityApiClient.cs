using Refit;

namespace ProtonDrive.Client.Instrumentation.Observability;

public interface IObservabilityApiClient
{
    [Post("/v1/metrics")]
    [BearerAuthorizationHeader]
    Task<ApiResponse> SendMetricsAsync(ObservabilityMetrics metrics, CancellationToken cancellationToken);
}
