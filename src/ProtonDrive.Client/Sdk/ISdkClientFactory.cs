using Proton.Drive.Sdk;

namespace ProtonDrive.Client.Sdk;

internal interface ISdkClientFactory
{
    public ProtonDriveClient GetOrCreateClient();
}
