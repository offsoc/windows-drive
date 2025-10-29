using ProtonDrive.Client.Contacts.Contracts;
using Refit;

namespace ProtonDrive.Client.Contacts;

public interface IContactApiClient
{
    [Get("/v4/emails")]
    [BearerAuthorizationHeader]
    Task<ContactListResponse> GetContactsAsync(CancellationToken cancellationToken);
}
