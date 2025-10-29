namespace ProtonDrive.Client.Contacts;

public interface IContactService
{
    Task<string?> GetDisplayNameByEmailAddressAsync(string email, CancellationToken cancellationToken);
}
