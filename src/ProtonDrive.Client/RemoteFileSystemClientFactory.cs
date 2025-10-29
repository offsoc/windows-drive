using Microsoft.Extensions.Logging;
using ProtonDrive.Client.BlockVerification;
using ProtonDrive.Client.Configuration;
using ProtonDrive.Client.Cryptography;
using ProtonDrive.Client.FileUploading;
using ProtonDrive.Client.MediaTypes;
using ProtonDrive.Client.RemoteNodes;
using ProtonDrive.Client.Volumes;
using ProtonDrive.Shared.Devices;
using ProtonDrive.Shared.Features;
using ProtonDrive.Shared.Reporting;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client;

internal sealed class RemoteFileSystemClientFactory : IRemoteFileSystemClientFactory
{
    private readonly DriveApiConfig _driveApi;
    private readonly IFeatureFlagProvider _featureFlagProvider;
    private readonly IFileContentTypeProvider _fileContentTypeProvider;
    private readonly IClientInstanceIdentityProvider _clientInstanceIdentityProvider;
    private readonly IRemoteNodeService _remoteNodeService;
    private readonly ILinkApiClient _linkApiClient;
    private readonly IFolderApiClient _folderApiClient;
    private readonly IFileApiClient _fileApiClient;
    private readonly IPhotoApiClient _photoApiClient;
    private readonly IVolumeApiClient _volumeApiClient;
    private readonly ICryptographyService _cryptographyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRevisionSealerFactory _revisionSealerFactory;
    private readonly IRevisionManifestCreator _revisionManifestCreator;
    private readonly IBlockVerifierFactory _blockVerifierFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Action<Exception> _reportBlockVerificationOrDecryptionFailure;

    public RemoteFileSystemClientFactory(
        DriveApiConfig driveApi,
        IFeatureFlagProvider featureFlagProvider,
        IFileContentTypeProvider fileContentTypeProvider,
        IClientInstanceIdentityProvider clientInstanceIdentityProvider,
        IRemoteNodeService remoteNodeService,
        ILinkApiClient linkApiClient,
        IFolderApiClient folderApiClient,
        IFileApiClient fileApiClient,
        IPhotoApiClient photoApiClient,
        IVolumeApiClient volumeApiClient,
        ICryptographyService cryptographyService,
        IHttpClientFactory httpClientFactory,
        IRevisionSealerFactory revisionSealerFactory,
        IRevisionManifestCreator revisionManifestCreator,
        IBlockVerifierFactory blockVerifierFactory,
        ILoggerFactory loggerFactory,
        IErrorReporting errorReporting)
    {
        _driveApi = driveApi;
        _featureFlagProvider = featureFlagProvider;
        _fileContentTypeProvider = fileContentTypeProvider;
        _clientInstanceIdentityProvider = clientInstanceIdentityProvider;
        _remoteNodeService = remoteNodeService;
        _linkApiClient = linkApiClient;
        _folderApiClient = folderApiClient;
        _fileApiClient = fileApiClient;
        _photoApiClient = photoApiClient;
        _volumeApiClient = volumeApiClient;
        _cryptographyService = cryptographyService;
        _httpClientFactory = httpClientFactory;
        _revisionSealerFactory = revisionSealerFactory;
        _revisionManifestCreator = revisionManifestCreator;
        _blockVerifierFactory = blockVerifierFactory;
        _loggerFactory = loggerFactory;
        _reportBlockVerificationOrDecryptionFailure = errorReporting.CaptureException;
    }

    public IFileSystemClient<string> CreateClient(FileSystemClientParameters parameters)
    {
        //TODO: Add SDK client when ready
        return CreateLegacyClient(parameters);
    }

    private RemoteFileSystemClient CreateLegacyClient(FileSystemClientParameters parameters)
    {
        return new RemoteFileSystemClient(
            _driveApi,
            parameters,
            _fileContentTypeProvider,
            _clientInstanceIdentityProvider,
            _remoteNodeService,
            _linkApiClient,
            _folderApiClient,
            _fileApiClient,
            _photoApiClient,
            _volumeApiClient,
            _cryptographyService,
            _httpClientFactory,
            _revisionSealerFactory,
            _revisionManifestCreator,
            _blockVerifierFactory,
            _loggerFactory,
            _reportBlockVerificationOrDecryptionFailure
        );
    }

    private SdkRemoteFileSystemClient CreateSdkClient(FileSystemClientParameters parameters)
    {
        throw new NotSupportedException();
    }
}
