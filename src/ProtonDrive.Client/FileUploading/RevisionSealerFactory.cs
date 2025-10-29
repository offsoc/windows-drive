using Microsoft.Extensions.Logging;
using ProtonDrive.Client.Cryptography;
using ProtonDrive.Client.Cryptography.Pgp;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal class RevisionSealerFactory : IRevisionSealerFactory
{
    private readonly IFileRevisionUpdateApiClient _fileRevisionUpdateApiClient;
    private readonly IPhotoHashProvider _photoHashProvider;
    private readonly IRevisionManifestCreator _revisionManifestCreator;
    private readonly ILogger<RevisionSealer> _logger;

    public RevisionSealerFactory(
        IFileRevisionUpdateApiClient fileRevisionUpdateApiClient,
        IPhotoHashProvider photoHashProvider,
        IRevisionManifestCreator revisionManifestCreator,
        ILogger<RevisionSealer> logger)
    {
        _fileRevisionUpdateApiClient = fileRevisionUpdateApiClient;
        _photoHashProvider = photoHashProvider;
        _revisionManifestCreator = revisionManifestCreator;
        _logger = logger;
    }

    public IRevisionSealer CreateRegularSealer(
        RevisionSealerParameters revisionSealerParameters,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IExtendedAttributesBuilder extendedAttributesBuilder)
    {
        return new RevisionSealer(
            revisionSealerParameters,
            signatureProducer,
            signatureAddress,
            _revisionManifestCreator,
            extendedAttributesBuilder,
            _fileRevisionUpdateApiClient,
            _logger);
    }

    public IRevisionSealer CreatePhotoSealer(
        RevisionSealerParameters revisionSealerParameters,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IExtendedAttributesBuilder extendedAttributesBuilder,
        IFileMetadataProvider fileMetadataProvider)
    {
        return new PhotoRevisionSealer(
            revisionSealerParameters,
            signatureProducer,
            signatureAddress,
            _revisionManifestCreator,
            extendedAttributesBuilder,
            _fileRevisionUpdateApiClient,
            _photoHashProvider,
            fileMetadataProvider,
            _logger);
    }
}
