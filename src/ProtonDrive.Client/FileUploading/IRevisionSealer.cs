using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Client.Contracts;

namespace ProtonDrive.Client.FileUploading;

internal interface IRevisionSealer
{
    Task SealRevisionAsync(
        IReadOnlyCollection<UploadedBlock> blocks,
        string sha1Digest,
        CancellationToken cancellationToken);
}
