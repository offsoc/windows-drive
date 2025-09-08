using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Photos;
using ProtonDrive.App.Services;
using ProtonDrive.Client;
using ProtonDrive.Client.Volumes.Contracts;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.Logging;
using ProtonDrive.Shared.Threading;

namespace ProtonDrive.App.Volumes;

internal sealed class PhotoVolumeService : IPhotoVolumeService, IStoppableService, IVolumeStateAware, IPhotosFeatureStateAware
{
    private readonly IActiveVolumeService _activeVolumeService;
    private readonly Lazy<IEnumerable<IPhotoVolumeStateAware>> _photoVolumeStateAware;
    private readonly ILogger<PhotoVolumeService> _logger;

    private readonly CoalescingAction _stateChangeHandler;
    private readonly ConcurrentStateHandler _photoVolumeStateCheck;
    private readonly ConcurrentStateHandler _photoVolumeSetup;

    private VolumeState _state = VolumeState.Idle;
    private bool _isStopping;

    public PhotoVolumeService(
        IActiveVolumeService activeVolumeService,
        Lazy<IEnumerable<IPhotoVolumeStateAware>> photoVolumeStateAware,
        ILogger<PhotoVolumeService> logger)
    {
        _activeVolumeService = activeVolumeService;
        _photoVolumeStateAware = photoVolumeStateAware;
        _logger = logger;

        _photoVolumeStateCheck = new ConcurrentStateHandler(SchedulePhotoVolumeStateCheck);
        _photoVolumeSetup = new ConcurrentStateHandler(SchedulePhotoVolumeSetup);

        _stateChangeHandler = _logger.GetCoalescingActionWithExceptionsLoggingAndCancellationHandling(HandleExternalStateChangeAsync, nameof(PhotoVolumeService));
    }

    async Task IStoppableService.StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"{nameof(PhotoVolumeService)} is stopping");

        _isStopping = true;
        _stateChangeHandler.Cancel();

        await WaitForCompletionAsync().ConfigureAwait(false);

        _logger.LogDebug($"{nameof(PhotoVolumeService)} stopped");
    }

    void IVolumeStateAware.OnVolumeStateChanged(VolumeState value)
    {
        if (value.Status is VolumeStatus.Ready)
        {
            _photoVolumeStateCheck.TryStart();
        }
        else
        {
            _photoVolumeStateCheck.TryCancel();
        }
    }

    void IPhotosFeatureStateAware.OnPhotosFeatureStateChanged(PhotosFeatureState value)
    {
        if (value.Status is PhotosFeatureStatus.SettingUp)
        {
            _photoVolumeSetup.TryStart();
        }
        else
        {
            _photoVolumeSetup.TryCancel();
        }
    }

    public Task RetryFailedSetupAsync()
    {
        _photoVolumeStateCheck.TryRestart();
        _photoVolumeSetup.TryRestart();

        return WaitForCompletionAsync();
    }

    internal Task WaitForCompletionAsync()
    {
        // Wait for all scheduled tasks to complete
        return _stateChangeHandler.WaitForCompletionAsync();
    }

    private void SchedulePhotoVolumeStateCheck(ConcurrentStateHandler.Status fromStatus, ConcurrentStateHandler.Status toStatus)
    {
        ScheduleStateChangeHandling(fromStatus, toStatus, cancelRunningAction: true);
    }

    private void SchedulePhotoVolumeSetup(ConcurrentStateHandler.Status fromStatus, ConcurrentStateHandler.Status toStatus)
    {
        ScheduleStateChangeHandling(fromStatus, toStatus, cancelRunningAction: false);
    }

    private void ScheduleStateChangeHandling(ConcurrentStateHandler.Status fromStatus, ConcurrentStateHandler.Status toStatus, bool cancelRunningAction)
    {
        if (_isStopping)
        {
            return;
        }

        if (toStatus is ConcurrentStateHandler.Status.Completed)
        {
            return;
        }

        if (cancelRunningAction && fromStatus is ConcurrentStateHandler.Status.Requested && toStatus is ConcurrentStateHandler.Status.NotRequested)
        {
            _stateChangeHandler.Cancel();
        }

        _stateChangeHandler.Run();
    }

    private async Task HandleExternalStateChangeAsync(CancellationToken cancellationToken)
    {
        if (_isStopping)
        {
            return;
        }

        if (_photoVolumeStateCheck.IsNotRequested)
        {
            SetState(VolumeStatus.Idle);
            return;
        }

        if (_photoVolumeStateCheck.IsRequested)
        {
            SetState(VolumeStatus.SettingUp);

            var result = await GetPhotoVolumeAsync(cancellationToken).ConfigureAwait(false);

            if (result is not null)
            {
                if (result.Value.Volume != null)
                {
                    SetState(VolumeStatus.Ready, result.Value.Volume);
                }
                else
                {
                    SetFailure(result.Value.ErrorMessage);
                }

                _photoVolumeStateCheck.TryComplete();
                return;
            }

            _photoVolumeStateCheck.TryComplete();
        }

        if (_photoVolumeSetup.IsRequested)
        {
            SetState(VolumeStatus.SettingUp);

            var (volume, errorMessage) = await CreatePhotoVolumeAsync(cancellationToken).ConfigureAwait(false);

            if (volume != null)
            {
                SetState(VolumeStatus.Ready, volume);
            }
            else
            {
                SetFailure(errorMessage);
            }

            _photoVolumeSetup.TryComplete();
            return;
        }

        if (_state.Status is VolumeStatus.SettingUp)
        {
            SetState(VolumeStatus.Idle);
        }
    }

    private async Task<(VolumeInfo? Volume, string? ErrorMessage)?> GetPhotoVolumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var volume = await _activeVolumeService.GetPhotoVolumeAsync(cancellationToken).ConfigureAwait(false);

            if (volume is null)
            {
                return null;
            }

            return (volume, ErrorMessage: null);
        }
        catch (Exception ex) when (ex.IsDriveClientException())
        {
            _logger.LogInformation(
                "Failed to get {Type} volume: {ErrorCode} {ErrorMessage}",
                VolumeType.Photo,
                ex.GetRelevantFormattedErrorCode(),
                ex.CombinedMessage());

            return (Volume: null, ex.Message);
        }
    }

    private async Task<(VolumeInfo? Volume, string? ErrorMessage)> CreatePhotoVolumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var volume = await _activeVolumeService.CreatePhotoVolumeAsync(cancellationToken).ConfigureAwait(false);

            return (volume, ErrorMessage: null);
        }
        catch (Exception ex) when (ex.IsDriveClientException())
        {
            _logger.LogError(
                "Failed to create {Type} volume: {ErrorCode} {ErrorMessage}",
                VolumeType.Photo,
                ex.GetRelevantFormattedErrorCode(),
                ex.CombinedMessage());

            return (Volume: null, ex.Message);
        }
    }

    private void SetState(VolumeStatus status, VolumeInfo? volume = null)
    {
        SetState(new VolumeState(status, Volume: volume));
    }

    private void SetFailure(string? errorMessage)
    {
        SetState(new VolumeState(VolumeStatus.Failed, Volume: null, errorMessage));
    }

    private void SetState(VolumeState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        OnStateChanged(state);
    }

    private void OnStateChanged(VolumeState value)
    {
        _logger.LogInformation("Photo volume state changed to {Status}", value.Status);

        foreach (var listener in _photoVolumeStateAware.Value)
        {
            listener.OnPhotoVolumeStateChanged(value);
        }
    }
}
