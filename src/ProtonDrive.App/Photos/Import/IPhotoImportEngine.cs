using System.Threading;
using System.Threading.Tasks;

namespace ProtonDrive.App.Photos.Import;

internal interface IPhotoImportEngine
{
    Task ImportAsync(ImportProgressCallbacks callbacks, CancellationToken cancellationToken);
}
