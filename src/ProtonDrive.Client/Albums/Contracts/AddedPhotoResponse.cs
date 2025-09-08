namespace ProtonDrive.Client.Albums.Contracts;

public sealed record AddedPhotoResponse : ApiResponse
{
    public AddedPhotoResponseDetails? Details { get; init; }
}
