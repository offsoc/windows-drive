namespace ProtonDrive.Client.Authentication.Sessions;

public interface ISessionClient
{
    Task<string> ForkSessionAsync(SessionForkingParameters parameters, CancellationToken cancellationToken);
}
