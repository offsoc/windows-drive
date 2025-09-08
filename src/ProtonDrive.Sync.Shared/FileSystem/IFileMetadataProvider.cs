using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IFileMetadataProvider
{
    DateTime CreationTimeUtc { get; }

    Task<FileMetadata?> GetMetadataAsync();

    Task<IReadOnlySet<PhotoTag>> GetPhotoTagsAsync(CancellationToken cancellationToken);
}
