using ProtonDrive.Client.Events;

namespace ProtonDrive.Client.Shares.Events;

internal interface IShareEventClient
{
    Task<DriveEvents> GetEventsAsync(string shareId, DriveEventResumeToken resumeToken, CancellationToken cancellationToken);
}
