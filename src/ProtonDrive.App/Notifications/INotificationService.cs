namespace ProtonDrive.App.Notifications;

public interface INotificationService
{
    event EventHandler<NotificationActivatedEventArgs> NotificationActivated;

    void ShowNotification(Notification notification);
    void RemoveNotificationGroup(string group);
    void RemoveNotification(string group, string id);
}
