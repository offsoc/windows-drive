using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Client.Photos.Contracts;
using ProtonDrive.Shared.Devices;

namespace ProtonDrive.Client.FileUploading;

internal sealed class PhotoDuplicateService : IPhotoDuplicateService
{
    private readonly IPhotoHashProvider _photoHashProvider;
    private readonly IPhotoApiClient _photoApiClient;
    private readonly IClientInstanceIdentityProvider _clientInstanceIdentityProvider;

    public PhotoDuplicateService(
        IPhotoHashProvider photoHashProvider,
        IPhotoApiClient photoApiClient,
        IClientInstanceIdentityProvider clientInstanceIdentityProvider)
    {
        _photoHashProvider = photoHashProvider;
        _photoApiClient = photoApiClient;
        _clientInstanceIdentityProvider = clientInstanceIdentityProvider;
    }

    public async Task<(string ContentHash, string Sha1Digest)> GetContentHashAndSha1DigestAsync(
        Stream source,
        string shareId,
        string parentLinkId,
        CancellationToken cancellationToken)
    {
        var hash = await SHA1.HashDataAsync(source, cancellationToken).ConfigureAwait(false);
        var sha1Digest = Convert.ToHexStringLower(hash);
        var contentHash = await _photoHashProvider.GetContentHashAsync(shareId, parentLinkId, sha1Digest, cancellationToken).ConfigureAwait(false);
        return (contentHash, sha1Digest);
    }

    public async Task<ILookup<string, PhotoNameCollision>> GetNameCollisionsAsync(
        string volumeId,
        string shareId,
        string parentLinkId,
        IEnumerable<string> fileNames,
        CancellationToken cancellationToken)
    {
        ConcurrentDictionary<string, string> nameHashesByHash = [];

        await Parallel.ForEachAsync(
                fileNames,
                new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Math.Max(4, Environment.ProcessorCount) },
                async (fileName, ct) =>
                {
                    var nameHash = await _photoHashProvider.GetNameHashAsync(shareId, parentLinkId, fileName, ct).ConfigureAwait(false);
                    nameHashesByHash.TryAdd(nameHash, fileName);
                })
            .ConfigureAwait(false);

        var nameHashes = nameHashesByHash.Keys.ToList();

        var photoDuplicationResponse = await _photoApiClient.GetDuplicatesAsync(
            volumeId,
            new PhotoDuplicationParameters { NameHashes = nameHashes },
            cancellationToken).ThrowOnFailure().ConfigureAwait(false);

        var clientId = _clientInstanceIdentityProvider.GetClientInstanceId();

        var duplicatesByFileName = new List<PhotoNameCollision>(photoDuplicationResponse.PhotoDuplicates.Count);

        foreach (var duplicateHash in photoDuplicationResponse.PhotoDuplicates)
        {
            var draftCreatedByAnotherClient = duplicateHash.LinkState is LinkState.Draft
                && !string.IsNullOrEmpty(duplicateHash.ClientId)
                && !string.Equals(clientId, duplicateHash.ClientId);

            if (duplicateHash.NameHash is null
                || !nameHashesByHash.TryGetValue(duplicateHash.NameHash, out var fileName)
                || (duplicateHash.ContentHash is null && !draftCreatedByAnotherClient))
            {
                continue;
            }

            duplicatesByFileName.Add(
                new PhotoNameCollision(duplicateHash.LinkId, fileName, duplicateHash.NameHash, duplicateHash.ContentHash, draftCreatedByAnotherClient));
        }

        // A file with the same name can be uploaded multiple times if its content differs.
        return duplicatesByFileName.ToLookup(x => x.FileName);
    }
}
