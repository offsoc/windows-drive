namespace ProtonDrive.App.Authentication;

public interface IStatefulSessionService
{
    Task StartSessionAsync();
    Task EndSessionAsync();
}
