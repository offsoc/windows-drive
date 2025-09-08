using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtonDrive.Client.FileUploading;

public interface IPhotoDuplicateService
{
    public Task<string> GetContentHash(Stream source, string shareId, string parentLinkId, CancellationToken cancellationToken);

    public Task<ILookup<string, PhotoDuplicate>> GetDuplicatesByFilenameAsync(
        string volumeId,
        string shareId,
        string parentLinkId,
        IEnumerable<string> fileNames,
        CancellationToken cancellationToken);
}
