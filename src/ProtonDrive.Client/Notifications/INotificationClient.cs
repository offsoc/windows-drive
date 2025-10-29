using ProtonDrive.Client.Notifications.Contracts;

namespace ProtonDrive.Client.Notifications;

public interface INotificationClient
{
    Task<IReadOnlyCollection<Notification>> GetNotificationsAsync(string userSubscriptionPlanCode, CancellationToken cancellationToken);
}
