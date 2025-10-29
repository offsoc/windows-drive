using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contacts.Contracts;

public sealed record ContactListResponse : ApiResponse
{
    private IReadOnlyCollection<Contact>? _contacts;

    [JsonPropertyName("ContactEmails")]
    public IReadOnlyCollection<Contact> Contacts
    {
        get => _contacts ??= [];
        init => _contacts = value;
    }

    public int Total { get; init; }
}
