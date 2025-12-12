using Proton.Drive.Sdk;
using Proton.Sdk.Telemetry;
using ProtonDrive.Client.Authentication;
using ProtonDrive.Shared.Devices;

namespace ProtonDrive.Client.Sdk;

internal sealed class SdkClientFactory : ISdkClientFactory, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccountClient _accountClient;
    private readonly IClientInstanceIdentityProvider _clientInstanceIdentityProvider;
    private readonly SdkFeatureFlagProvider _sdkFeatureFlagProvider;
    private readonly ITelemetry _sdkDiagnostics;

    private DisposableProtonDriveClient? _sdkClient;
    private bool _sessionStarted;

    public SdkClientFactory(
        IAuthenticationService authenticationService,
        IHttpClientFactory httpClientFactory,
        IAccountClient accountClient,
        IClientInstanceIdentityProvider clientInstanceIdentityProvider,
        SdkFeatureFlagProvider sdkSdkFeatureFlagProvider,
        ITelemetry sdkDiagnostics)
    {
        _httpClientFactory = new SdkHttpClientFactoryDecorator(httpClientFactory);
        _accountClient = accountClient;
        _clientInstanceIdentityProvider = clientInstanceIdentityProvider;
        _sdkFeatureFlagProvider = sdkSdkFeatureFlagProvider;
        _sdkDiagnostics = sdkDiagnostics;

        authenticationService.SessionStarted += (_, _) => _sessionStarted = true;
    }

    public ProtonDriveClient GetOrCreateClient()
    {
        if (_sessionStarted)
        {
            _sdkClient?.Dispose();
            _sdkClient = null;
            _sessionStarted = false;
        }

        return (_sdkClient ??= CreateClient()).Instance;
    }

    public void Dispose()
    {
        _sdkClient?.Dispose();
    }

    private DisposableProtonDriveClient CreateClient()
    {
        return new DisposableProtonDriveClient(
            _httpClientFactory,
            _accountClient,
            _clientInstanceIdentityProvider,
            _sdkFeatureFlagProvider,
            _sdkDiagnostics);
    }
}
