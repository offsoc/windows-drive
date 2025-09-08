using Microsoft.Extensions.Logging;
using Proton.Security.Cryptography.Abstractions;
using ProtonDrive.Client.Cryptography;

namespace ProtonDrive.Client.FileUploading;

internal class RevisionSealerFactory : IRevisionSealerFactory
{
    private readonly IFileRevisionUpdateApiClient _fileRevisionUpdateApiClient;
    private readonly IRevisionManifestCreator _revisionManifestCreator;
    private readonly ILogger<RevisionSealer> _logger;

    public RevisionSealerFactory(
        IFileRevisionUpdateApiClient fileRevisionUpdateApiClient,
        IRevisionManifestCreator revisionManifestCreator,
        ILogger<RevisionSealer> logger)
    {
        _fileRevisionUpdateApiClient = fileRevisionUpdateApiClient;
        _revisionManifestCreator = revisionManifestCreator;
        _logger = logger;
    }

    public IRevisionSealer Create(
        string shareId,
        string linkId,
        string revisionId,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IExtendedAttributesBuilder extendedAttributesBuilder)
    {
        return new RevisionSealer(
            shareId,
            linkId,
            revisionId,
            signatureProducer,
            signatureAddress,
            _revisionManifestCreator,
            extendedAttributesBuilder,
            _fileRevisionUpdateApiClient,
            _logger);
    }
}
