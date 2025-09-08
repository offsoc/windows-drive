using System.Threading;
using System.Threading.Tasks;

namespace ProtonDrive.Client.FileUploading;

internal interface IRevisionSealer
{
    Task SealRevisionAsync(RevisionSealingParameters parameters, CancellationToken cancellationToken);
}
