namespace ProtonDrive.Client.Photos.Contracts;

public sealed record PhotoDuplicationParameters
{
    public IReadOnlyCollection<string> NameHashes { get; init; } = [];
}
