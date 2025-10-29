namespace ProtonDrive.App.Services;

public interface IStoppableService
{
    public Task StopAsync(CancellationToken cancellationToken);
}
