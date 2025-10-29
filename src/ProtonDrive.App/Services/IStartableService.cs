namespace ProtonDrive.App.Services;

public interface IStartableService
{
    public Task StartAsync(CancellationToken cancellationToken);
}
