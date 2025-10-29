using Refit;

namespace ProtonDrive.Client.Authentication.Sessions;

internal interface IAuthenticationSessionApiClient
{
    [Post("/v4/sessions/forks")]
    [BearerAuthorizationHeader]
    Task<SessionForkingResponse> ForkSessionAsync(SessionForkingParameters parameters, CancellationToken cancellationToken);

    [Delete("/v4/sessions/forks/{sessionSelector}")]
    [BearerAuthorizationHeader]
    Task<ApiResponse> InvalidateSessionForkAsync(string sessionSelector, CancellationToken cancellationToken);
}
