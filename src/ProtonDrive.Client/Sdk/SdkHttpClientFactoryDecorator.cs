using ProtonDrive.Client.Configuration;

namespace ProtonDrive.Client.Sdk;

internal sealed class SdkHttpClientFactoryDecorator(IHttpClientFactory instanceToDecorate) : IHttpClientFactory
{
    private readonly IHttpClientFactory _decoratedInstance = instanceToDecorate;

    public HttpClient CreateClient(string name)
    {
        return _decoratedInstance.CreateClient(ApiClientConfigurator.SdkHttpClientName);
    }
}
