namespace ProtonDrive.Client.Authentication;

internal interface ISessionProvider
{
    Task<(Session Session, Func<CancellationToken, Task<Session?>> GetRefreshedSessionAsync)?> GetSessionAsync(CancellationToken cancellationToken);
}
