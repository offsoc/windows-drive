using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.App.Account;
using ProtonDrive.App.Services;
using ProtonDrive.Client;
using ProtonDrive.Client.Volumes.Contracts;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.Threading;

namespace ProtonDrive.App.Volumes;

internal sealed class VolumeService : IAccountStateAware, IVolumeService, IStoppableService
{
    private readonly IActiveVolumeService _activeVolumeService;
    private readonly Lazy<IEnumerable<IVolumeStateAware>> _volumeStateAware;
    private readonly ILogger<VolumeService> _logger;

    private readonly CancellationHandle _cancellationHandle = new();
    private readonly IScheduler _scheduler;

    private VolumeState _state = VolumeState.Idle;
    private AccountStatus _accountStatus;
    private bool _stopping;

    public VolumeService(
        IActiveVolumeService activeVolumeService,
        Lazy<IEnumerable<IVolumeStateAware>> volumeStateAware,
        ILogger<VolumeService> logger)
    {
        _activeVolumeService = activeVolumeService;
        _volumeStateAware = volumeStateAware;
        _logger = logger;

        _scheduler =
            new HandlingCancellationSchedulerDecorator(
                nameof(VolumeService),
                logger,
                new LoggingExceptionsSchedulerDecorator(
                    nameof(VolumeService),
                    logger,
                    new SerialScheduler()));
    }

    public VolumeState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                OnStateChanged(value);
            }
        }
    }

    /// <summary>
    /// Get the active volume and cache it internally to avoid multiple API calls.
    /// When the user signs out, the cache is cleared.
    /// </summary>
    /// <returns>The active volume or Null if there is no active volume.</returns>
    public async Task<VolumeInfo?> GetActiveVolumeAsync()
    {
        return GetCachedVolume() ?? await GetVolumeAsync().ConfigureAwait(false);
    }

    void IAccountStateAware.OnAccountStateChanged(AccountState value)
    {
        _accountStatus = value.Status;

        if (value.Status == AccountStatus.Succeeded)
        {
            _logger.LogDebug("Scheduling user volume set up");
            Schedule(SetUpVolumeAsync);
        }
        else
        {
            if (State.Status != VolumeStatus.Idle)
            {
                _logger.LogDebug("Scheduling cancellation of user volume set up");
            }

            _cancellationHandle.Cancel();
            Schedule(CancelSetupAsync);
        }
    }

    async Task IStoppableService.StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"{nameof(VolumeService)} is stopping");

        _stopping = true;
        _cancellationHandle.Cancel();

        // Wait for all scheduled tasks to complete
        await WaitForCompletionAsync().ConfigureAwait(false);

        _logger.LogDebug($"{nameof(VolumeService)} stopped");
    }

    internal Task WaitForCompletionAsync()
    {
        // Wait for all currently scheduled tasks to complete
        return _scheduler.Schedule(() => { });
    }

    private VolumeInfo? GetCachedVolume()
    {
        var (status, volume, _) = State;

        return status is VolumeStatus.Ready ? volume : null;
    }

    private async Task<VolumeInfo?> GetVolumeAsync()
    {
        await Schedule(SetUpVolumeAsync).ConfigureAwait(false);

        return State.Volume;
    }

    private async Task SetUpVolumeAsync(CancellationToken cancellationToken)
    {
        if (_stopping ||
            _accountStatus is not AccountStatus.Succeeded ||
            State.Status is VolumeStatus.Ready)
        {
            return;
        }

        SetStatus(VolumeStatus.SettingUp);

        var (volume, errorMessage) =
            (await GetVolumeAsync(cancellationToken).ConfigureAwait(false)) ??
            await CreateVolumeAsync(cancellationToken).ConfigureAwait(false);

        if (volume != null)
        {
            SetSuccess(volume);
        }
        else
        {
            SetStatus(VolumeStatus.Failed, errorMessage);
        }
    }

    private Task CancelSetupAsync(CancellationToken cancellationToken)
    {
        if (_stopping)
        {
            return Task.CompletedTask;
        }

        if (State.Status != VolumeStatus.Idle)
        {
            _logger.LogInformation("Setting up user volume has been cancelled");
        }

        SetStatus(VolumeStatus.Idle);

        return Task.CompletedTask;
    }

    private async Task<(VolumeInfo? Volume, string? ErrorMessage)?> GetVolumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var volume = await _activeVolumeService.GetMainVolumeAsync(cancellationToken).ConfigureAwait(false);

            if (volume is null)
            {
                return null;
            }

            return (volume, ErrorMessage: null);
        }
        catch (Exception ex) when (ex.IsDriveClientException())
        {
            _logger.LogInformation("Failed to get {Type} volume: {Message}", VolumeType.Main, ex.CombinedMessage());

            return (Volume: null, ex.Message);
        }
    }

    private async Task<(VolumeInfo? Volume, string? ErrorMessage)> CreateVolumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return (await _activeVolumeService.GetMainVolumeAsync(cancellationToken).ConfigureAwait(false), ErrorMessage: null);
        }
        catch (Exception ex) when (ex.IsDriveClientException())
        {
            _logger.LogError("Failed to create {Type} volume: {Message}", VolumeType.Main, ex.CombinedMessage());

            return (Volume: null, ex.Message);
        }
    }

    private void SetStatus(VolumeStatus status, string? errorMessage = null)
    {
        State = new VolumeState(status, status is not VolumeStatus.Idle ? State.Volume : null, errorMessage);
    }

    private void SetSuccess(VolumeInfo volume)
    {
        State = new VolumeState(VolumeStatus.Ready, volume);
    }

    private void OnStateChanged(VolumeState value)
    {
        _logger.LogInformation("Volume state changed to {Status}", value.Status);

        foreach (var listener in _volumeStateAware.Value)
        {
            listener.OnVolumeStateChanged(value);
        }
    }

    private Task Schedule(Func<CancellationToken, Task> action)
    {
        if (_stopping)
        {
            return Task.CompletedTask;
        }

        return _scheduler.Schedule(action, _cancellationHandle.Token);
    }
}
