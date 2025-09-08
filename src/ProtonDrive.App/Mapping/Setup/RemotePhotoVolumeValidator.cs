using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Drive.Services;
using ProtonDrive.App.Settings;
using ProtonDrive.App.Volumes;
using ProtonDrive.Client;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.App.Mapping.Setup;

internal sealed class RemotePhotoVolumeValidator : IRemotePhotoVolumeValidator, IPhotoVolumeStateAware
{
    private readonly IRemoteFolderService _remoteFolderService;
    private readonly ILogger<RemotePhotoVolumeValidator> _logger;

    private VolumeState _photoVolumeState = VolumeState.Idle;

    public RemotePhotoVolumeValidator(
        IRemoteFolderService remoteFolderService,
        ILogger<RemotePhotoVolumeValidator> logger)
    {
        _remoteFolderService = remoteFolderService;
        _logger = logger;
    }

    public MappingErrorCode? ValidateIdentity(RemoteReplica replica)
    {
        var (status, volume, _) = _photoVolumeState;

        if (status is not VolumeStatus.Ready)
        {
            _logger.LogWarning("Photo volume is not ready, status is {VolumeStatus}", status);

            return MappingErrorCode.PhotosNotReady;
        }

        Ensure.NotNull(volume, nameof(volume));

        if (replica.VolumeId != volume.Id)
        {
            _logger.LogWarning("Photo volume has diverged");
            return MappingErrorCode.DriveVolumeDiverged;
        }

        if (replica.ShareId != volume.RootShareId)
        {
            _logger.LogWarning("Photo volume root share has diverged");
            return MappingErrorCode.DriveShareDiverged;
        }

        if (replica.RootLinkId != volume.RootLinkId)
        {
            _logger.LogWarning("Photo volume root folder has diverged");
            return MappingErrorCode.DriveFolderDiverged;
        }

        return null;
    }

    public async Task<MappingErrorCode?> CheckAccessibilityAsync(RemoteReplica replica, CancellationToken cancellationToken)
    {
        Ensure.NotNullOrEmpty(replica.ShareId, nameof(replica), nameof(replica.ShareId));
        Ensure.NotNullOrEmpty(replica.RootLinkId, nameof(replica), nameof(replica.RootLinkId));

        try
        {
            var folderExists = await _remoteFolderService.FolderExistsAsync(replica.ShareId, replica.RootLinkId, cancellationToken).ConfigureAwait(false);

            if (!folderExists)
            {
                _logger.LogWarning("Photo volume root folder does not exist");
                return MappingErrorCode.DriveFolderDiverged;
            }
        }
        catch (Exception ex) when (ex.IsDriveClientException())
        {
            _logger.LogWarning("Failed to access remote photo volume: {ErrorCode} {ErrorMessage}", ex.GetRelevantFormattedErrorCode(), ex.CombinedMessage());
            return MappingErrorCode.DriveAccessFailed;
        }

        return null;
    }

    void IPhotoVolumeStateAware.OnPhotoVolumeStateChanged(VolumeState value)
    {
        _photoVolumeState = value;
    }
}
