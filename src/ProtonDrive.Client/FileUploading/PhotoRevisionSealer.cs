using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proton.Security.Cryptography.Abstractions;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Client.Cryptography;
using ProtonDrive.Client.Photos.Contracts;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal sealed class PhotoRevisionSealer : RevisionSealer
{
    private readonly IExtendedAttributesBuilder _extendedAttributesBuilder;
    private readonly IPhotoHashProvider _photoHashProvider;
    private readonly IFileMetadataProvider _fileMetadataProvider;

    private readonly string _parentLinkId;
    private readonly string _shareId;

    public PhotoRevisionSealer(
        RevisionSealerParameters parameters,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IRevisionManifestCreator revisionManifestCreator,
        IExtendedAttributesBuilder extendedAttributesBuilder,
        IFileRevisionUpdateApiClient fileRevisionUpdateApiClient,
        IPhotoHashProvider photoHashProvider,
        IFileMetadataProvider fileMetadataProvider,
        ILogger<RevisionSealer> logger)
        : base(
            parameters,
            signatureProducer,
            signatureAddress,
            revisionManifestCreator,
            extendedAttributesBuilder,
            fileRevisionUpdateApiClient,
            logger)
    {
        _parentLinkId = parameters.ParentLinkId ?? throw new ArgumentNullException(nameof(parameters), "ParentLinkId is required");
        _shareId = parameters.ShareId;
        _extendedAttributesBuilder = extendedAttributesBuilder;
        _photoHashProvider = photoHashProvider;
        _fileMetadataProvider = fileMetadataProvider;
    }

    protected override async Task<RevisionUpdateParameters> GetRevisionParametersAsync(
        string manifestSignature,
        string? extendedAttributes,
        DateTime creationTimeUtc,
        string sha1Digest,
        CancellationToken cancellationToken)
    {
        var parameters = await base.GetRevisionParametersAsync(manifestSignature, extendedAttributes, creationTimeUtc, sha1Digest, cancellationToken)
            .ConfigureAwait(false);

        var contentHash = await _photoHashProvider.GetContentHashAsync(_shareId, _parentLinkId, sha1Digest, cancellationToken).ConfigureAwait(false);
        var captureTime = (_extendedAttributesBuilder.CaptureTime ?? creationTimeUtc).ToUnixTimeSeconds();
        var photoTags = await _fileMetadataProvider.GetPhotoTagsAsync(cancellationToken).ConfigureAwait(false);

        parameters.PhotoDetails = new PhotoRevisionDetails(captureTime, contentHash, MainPhotoLinkId: null, photoTags);
        return parameters;
    }
}
