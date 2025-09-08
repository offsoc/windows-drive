using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoreLinq;
using ProtonDrive.App.Account;
using ProtonDrive.App.Services;
using ProtonDrive.Client;
using ProtonDrive.Client.Configuration;
using ProtonDrive.Client.Features;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Configuration;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.Features;
using ProtonDrive.Shared.Repository;
using ProtonDrive.Shared.Threading;

namespace ProtonDrive.App.Features;

public sealed class FeatureService : IFeatureFlagProvider, IStartableService, IAccountSwitchingAware, IAccountStateAware
{
    // If the API is not returning a feature flag/kill switch, we can safely consider it as "disabled".
    private const bool FallbackValueForMissingFeatureFlag = false;

    private readonly IFeatureApiClient _featureApiClient;
    private readonly IRepository<IReadOnlyDictionary<Feature, bool>> _repository;
    private readonly ILogger<FeatureService> _logger;

    private readonly IReadOnlyDictionary<Feature, bool> _localFeatureOverrides;
    private readonly CancellationHandle _cancellationHandle = new();
    private readonly TimeSpan _period;
    private readonly Lazy<IEnumerable<IFeatureFlagsAware>> _featureFlagsAware;
    private readonly Func<TimeSpan, IPeriodicTimer> _periodicTimerFactory;

    private IReadOnlyDictionary<Feature, bool> _cachedFeatureFlags = new Dictionary<Feature, bool>();

    private IPeriodicTimer _timer;
    private Task? _timerTask;
    private Task? _firstRefreshFeaturesTask;
    private bool _featuresFetchedAtLeastOnce;

    public FeatureService(
        DriveApiConfig config,
        FeatureFlags localFeatureFlags,
        IFeatureApiClient featureApiClient,
        IRepository<IReadOnlyDictionary<Feature, bool>> featureRepository,
        Lazy<IEnumerable<IFeatureFlagsAware>> featureFlagsAware,
        Func<TimeSpan, IPeriodicTimer> periodicTimerFactory,
        ILogger<FeatureService> logger)
    {
        _featureApiClient = featureApiClient;
        _repository = featureRepository;
        _logger = logger;
        _featureFlagsAware = featureFlagsAware;
        _periodicTimerFactory = periodicTimerFactory;

        _localFeatureOverrides = GetLocalFeatureOverrides(localFeatureFlags).ToFrozenDictionary(x => x.Feature, x => x.IsEnabled);
        _period = config.FeaturesUpdateInterval.RandomizedWithDeviation(0.2);
        _timer = periodicTimerFactory.Invoke(_period);
    }

    Task IStartableService.StartAsync(CancellationToken cancellationToken)
    {
        LoadCache();

        return Task.CompletedTask;
    }

    void IAccountSwitchingAware.OnAccountSwitched()
    {
        LoadCache();
    }

    void IAccountStateAware.OnAccountStateChanged(AccountState value)
    {
        if (value.Status is AccountStatus.Succeeded)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    public async Task<bool> IsEnabledAsync(Feature feature, CancellationToken cancellationToken)
    {
        await EnsureFeaturesAreRetrievedAtLeastOnce(cancellationToken).ConfigureAwait(false);

        return IsEnabled(feature);
    }

    private static IEnumerable<(Feature Feature, bool IsEnabled)> GetLocalFeatureOverrides(FeatureFlags localFeatureFlags)
    {
        /* There are two cases of how local feature flag (there is no local kill switch) affects
         * remote feature flag values:
         * - If the remote feature flag exists, the enabled local feature flag overrides the remote feature flag value
         * by making it enabled. If remote kill switch exists, it's value is not affected.
         * - If the remote feature flag does not exist, but kill switch exists, the disabled local feature flag
         * overrides the remote kill switch value and makes it enabled.
         */

        yield break;
    }

    private async Task EnsureFeaturesAreRetrievedAtLeastOnce(CancellationToken cancellationToken)
    {
        if (_featuresFetchedAtLeastOnce)
        {
            return;
        }

        if (_firstRefreshFeaturesTask is null)
        {
            return;
        }

        if (_firstRefreshFeaturesTask.Status is not TaskStatus.RanToCompletion)
        {
            await _firstRefreshFeaturesTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private bool IsEnabled(Feature feature)
    {
        return
            (_localFeatureOverrides.TryGetValue(feature, out var enabled) && enabled) ||
            (_cachedFeatureFlags.TryGetValue(feature, out enabled) && enabled);
    }

    private void Start()
    {
        if (_timerTask is not null)
        {
            return; // Task already started
        }

        _logger.LogInformation("Feature service is starting");
        _timer = _periodicTimerFactory.Invoke(_period);
        _timerTask = GetTimerTaskAsync(_cancellationHandle.Token);
    }

    private void Stop()
    {
        if (_timerTask is null)
        {
            return;
        }

        _logger.LogInformation("Feature service is stopping");
        _timerTask = null;
        _firstRefreshFeaturesTask = null;
        _cancellationHandle.Cancel();
        _timer.Dispose();
    }

    private async Task GetTimerTaskAsync(CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                var task = RefreshFeatureFlagsAsync(cancellationToken);
                _firstRefreshFeaturesTask ??= task;

                await task.ConfigureAwait(false);
                _featuresFetchedAtLeastOnce = true;
            }
            while (await _timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            /* Do nothing */
        }
    }

    private async Task RefreshFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var featureListResponse = await _featureApiClient.GetFeaturesAsync(cancellationToken).ThrowOnFailure().ConfigureAwait(false);

            var latestFeatureFlags = featureListResponse.FeatureFlags
                .Select(
                    x => Enum.TryParse(x.Name, out Feature featureFlag)
                        ? new (Feature Feature, bool IsEnabled)?((featureFlag, x.Enabled))
                        : null)
                .Where(x => x is not null)
                .ToDictionary(x => x!.Value.Feature, y => y!.Value.IsEnabled);

            UpdateFeatureFlags(latestFeatureFlags);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Failed to refresh features flags: {ErrorCode} {ErrorMessage}", ex.ResponseCode, ex.CombinedMessage());
        }
    }

    private void UpdateFeatureFlags(Dictionary<Feature, bool> latestFeatureFlags)
    {
        var featureFlagComparisons = latestFeatureFlags.FullJoin(
            _cachedFeatureFlags,
            x => x.Key,
            y => y.Key,
            x => (Feature: x.Key, IsEnabled: x.Value, WasEnabled: FallbackValueForMissingFeatureFlag),
            y => (Feature: y.Key, IsEnabled: FallbackValueForMissingFeatureFlag, WasEnabled: y.Value),
            (x, y) => (Feature: x.Key, IsEnabled: x.Value, WasEnabled: y.Value)).ToList();

        var changedFeatureFlags = featureFlagComparisons.Where(x => x.IsEnabled != x.WasEnabled);

        if (!changedFeatureFlags.Any())
        {
            return;
        }

        _cachedFeatureFlags = latestFeatureFlags.AsReadOnly();
        NotifyFeatureFlagsChange();
        _repository.Set(_cachedFeatureFlags);
    }

    private void LoadCache()
    {
        var flags = _repository.Get();

        _featuresFetchedAtLeastOnce = flags is not null;

        _cachedFeatureFlags = flags ?? ImmutableDictionary<Feature, bool>.Empty;

        NotifyFeatureFlagsChange();
    }

    private void NotifyFeatureFlagsChange()
    {
        var featureOverrides = _localFeatureOverrides.Select(x => (x.Key, x.Value));
        var cachedFeatureFlags = _cachedFeatureFlags.Select(x => (x.Key, x.Value));

        var featureFlags = featureOverrides.UnionBy(cachedFeatureFlags, x => x.Key);

        OnFeatureFlagsChanged(featureFlags.ToList().AsReadOnly());
    }

    private void OnFeatureFlagsChanged(IReadOnlyCollection<(Feature Feature, bool IsEnabled)> features)
    {
        foreach (var listener in _featureFlagsAware.Value)
        {
            listener.OnFeatureFlagsChanged(features);
        }
    }
}
