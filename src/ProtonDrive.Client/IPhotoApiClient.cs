using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Client.Albums.Contracts;
using ProtonDrive.Client.Photos.Contracts;
using Refit;

namespace ProtonDrive.Client;

public interface IPhotoApiClient
{
    [Post("/photos/volumes/{volumeId}/albums")]
    [BearerAuthorizationHeader]
    public Task<AlbumCreationResponse> CreateAlbumAsync(string volumeId, AlbumCreationParameters parameters, CancellationToken cancellationToken);

    [Post("/photos/volumes/{volumeId}/albums/{albumLinkId}/add-multiple")]
    [BearerAuthorizationHeader]
    public Task<AddedPhotoResponseList> AddPhotosToAlbumAsync(
        string volumeId,
        string albumLinkId,
        PhotoToAddListParameters parameters,
        CancellationToken cancellationToken);

    [Post("/volumes/{volumeId}/photos/duplicates")]
    [BearerAuthorizationHeader]
    public Task<PhotoDuplicationResponse> GetDuplicatesAsync(string volumeId, PhotoDuplicationParameters parameters, CancellationToken cancellationToken);
}
