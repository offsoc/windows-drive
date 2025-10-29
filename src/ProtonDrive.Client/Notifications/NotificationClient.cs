using ProtonDrive.Client.Notifications.Contracts;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.Repository;

namespace ProtonDrive.Client.Notifications;

internal sealed class NotificationClient : INotificationClient
{
    private readonly ICollectionRepository<Notification> _repository;

    public NotificationClient(ICollectionRepository<Notification> repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<Notification>> GetNotificationsAsync(string userSubscriptionPlanCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var notifications =
                _repository
                    .GetAll()
                    .Where(n => n.UserSubscriptionPlanCodes.Contains(userSubscriptionPlanCode))
                    .ToList()
                    .AsReadOnly();

            return Task.FromResult<IReadOnlyCollection<Notification>>(notifications);
        }
        catch (Exception ex) when (ex.IsFileAccessException())
        {
            throw new ApiException("Retrieving notifications failed", ex);
        }
    }
}
