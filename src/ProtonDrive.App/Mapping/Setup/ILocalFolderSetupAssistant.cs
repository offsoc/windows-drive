using System.Threading;
using ProtonDrive.App.Settings;

namespace ProtonDrive.App.Mapping.Setup;

internal interface ILocalFolderSetupAssistant
{
    MappingErrorCode? SetUpLocalFolder(RemoteToLocalMapping mapping, CancellationToken cancellationToken);
}
