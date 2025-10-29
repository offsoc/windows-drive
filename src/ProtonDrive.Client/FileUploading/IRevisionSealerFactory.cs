using Proton.Security.Cryptography.Abstractions;
using ProtonDrive.Client.Cryptography;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal interface IRevisionSealerFactory
{
    IRevisionSealer CreateRegularSealer(
        RevisionSealerParameters revisionSealerParameters,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IExtendedAttributesBuilder extendedAttributesBuilder);

    IRevisionSealer CreatePhotoSealer(
        RevisionSealerParameters revisionSealerParameters,
        IPgpSignatureProducer signatureProducer,
        Address signatureAddress,
        IExtendedAttributesBuilder extendedAttributesBuilder,
        IFileMetadataProvider fileMetadataProvider);
}
