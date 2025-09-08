using System.Text.Json.Serialization;
using ProtonDrive.Client.Contracts;

namespace ProtonDrive.Client.Photos.Contracts;

public sealed record PhotoDuplicateDto
{
    /// <summary>
    /// NameHash of the found duplicate
    /// </summary>
    [JsonPropertyName("Hash")]
    public string? NameHash { get; init; }

    /// <summary>
    /// ContentHash of the found duplicate
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// It can be null if the link was deleted
    /// </summary>
    public LinkState? LinkState { get; init; }

    /// <summary>
    /// Client defined UID for the draft.
    /// <remarks>It can be null if no ClientUID was passed, or if the revision was already committed.</remarks>
    /// </summary>
    [JsonPropertyName("ClientUID")]
    public string? ClientId { get; init; }

    /// <summary>
    /// It can be null if the link was deleted
    /// </summary>
    [JsonPropertyName("LinkID")]
    public string? LinkId { get; init; }

    /// <summary>
    /// It can be null if the link was deleted
    /// </summary>
    [JsonPropertyName("RevisionID")]
    public string? RevisionId { get; init; }
}
