namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AlbumCreationResponse : ApiResponse
{
    public required AlbumShortDto Album { get; init; }
}
