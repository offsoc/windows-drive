namespace ProtonDrive.App.InterProcessCommunication;

public interface IIpcResponder
{
    Task Respond<T>(T value, CancellationToken cancellationToken);
}
