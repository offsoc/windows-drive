namespace ProtonDrive.App.Notifications;

public class NotificationActivatedEventArgs : EventArgs
{
    public string? Id { get; init; }
    public string? GroupId { get; init; }
    public string? Action { get; init; }
}
