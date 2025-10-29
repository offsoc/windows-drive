namespace ProtonDrive.App;

public interface IApp
{
    Task<IntPtr> ActivateAsync();

    Task RestartAsync();

    Task ExitAsync();
}
