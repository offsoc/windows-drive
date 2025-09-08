using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proton.Security.Cryptography.Abstractions;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Client.Cryptography;

namespace ProtonDrive.Client.FileUploading;

internal class RevisionSealer : IRevisionSealer
{
    private readonly string _shareId;
    private readonly string _fileId;
    private readonly string _revisionId;
    private readonly IPgpSignatureProducer _signatureProducer;
    private readonly Address _signatureAddress;
    private readonly IRevisionManifestCreator _revisionManifestCreator;
    private readonly IExtendedAttributesBuilder _extendedAttributesBuilder;
    private readonly IFileRevisionUpdateApiClient _fileRevisionUpdateApiClient;
    private readonly ILogger<RevisionSealer> _logger;

    public RevisionSealer(
        RevisionSealerParameters parameters,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IRevisionManifestCreator revisionManifestCreator,
        IExtendedAttributesBuilder extendedAttributesBuilder,
        IFileRevisionUpdateApiClient fileRevisionUpdateApiClient,
        ILogger<RevisionSealer> logger)
    {
        _fileRevisionUpdateApiClient = fileRevisionUpdateApiClient;
        _logger = logger;
        _signatureProducer = signatureProducer;
        _shareId = parameters.ShareId;
        _fileId = parameters.FileId;
        _revisionId = parameters.RevisionId;
        _signatureAddress = signatureAddress;
        _revisionManifestCreator = revisionManifestCreator;
        _extendedAttributesBuilder = extendedAttributesBuilder;
    }

    public async Task SealRevisionAsync(
        IReadOnlyCollection<UploadedBlock> blocks,
        DateTime creationTimeUtc,
        string sha1Digest,
        CancellationToken cancellationToken)
    {
        var manifest = _revisionManifestCreator.CreateManifest(blocks);

        var manifestSignature = _signatureProducer.SignWithArmor(manifest);

        _extendedAttributesBuilder.BlockSizes = blocks
            .Where(block => !block.IsThumbnail)
            .OrderBy(block => block.Index)
            .Select(block => block.NumberOfPlainDataBytesRead);

        _extendedAttributesBuilder.Sha1Digest = sha1Digest;

        var extendedAttributes = await _extendedAttributesBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);

        var parameters = await GetRevisionParametersAsync(manifestSignature, extendedAttributes, creationTimeUtc, sha1Digest, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("File with ID {FileID} SHA1 checksum is {SHA1}", _fileId, sha1Digest);

        await _fileRevisionUpdateApiClient
            .UpdateRevisionAsync(_shareId, _fileId, _revisionId, parameters, cancellationToken)
            .ThrowOnFailure()
            .ConfigureAwait(false);
    }

    protected virtual Task<RevisionUpdateParameters> GetRevisionParametersAsync(
        string manifestSignature,
        string? extendedAttributes,
        DateTime creationTimeUtc,
        string sha1Digest,
        CancellationToken cancellationToken)
    {
        var revisionUpdateParameters = new RevisionUpdateParameters(manifestSignature, _signatureAddress.EmailAddress, extendedAttributes);
        return Task.FromResult(revisionUpdateParameters);
    }
}
