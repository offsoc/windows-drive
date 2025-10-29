namespace ProtonDrive.Client.Contracts;

public sealed record LinkResponseList : ApiResponse
{
    public IReadOnlyList<Link> Parents { get; init; } = [];

    public IReadOnlyList<Link> Links { get; init; } = [];
}
