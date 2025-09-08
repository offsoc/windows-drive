using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Sync.Shared.FileSystem;
using ProtonDrive.Sync.Windows.FileSystem.Photos;

namespace ProtonDrive.Sync.Windows.FileSystem.Client;

internal sealed class FileRevision : IRevision
{
    private static readonly byte[] ReadabilityCheckBuffer = new byte[1];

    private readonly FileSystemFile _file;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IFileMetadataGenerator _fileMetadataGenerator;
    private readonly IPhotoTagsGenerator _photoTagsGenerator;

    private Stream? _stream;

    public FileRevision(
        FileSystemFile file,
        IThumbnailGenerator thumbnailGenerator,
        IFileMetadataGenerator fileMetadataGenerator,
        IPhotoTagsGenerator photoTagsGenerator)
    {
        _file = file;
        _thumbnailGenerator = thumbnailGenerator;
        _fileMetadataGenerator = fileMetadataGenerator;
        _photoTagsGenerator = photoTagsGenerator;

        Size = _file.Size;
        CreationTimeUtc = _file.CreationTimeUtc;
        LastWriteTimeUtc = _file.LastWriteTimeUtc;
    }

    public long Size { get; }
    public DateTime CreationTimeUtc { get; }
    public DateTime LastWriteTimeUtc { get; }

    public async Task CheckReadabilityAsync(CancellationToken cancellationToken)
    {
        var stream = GetContentStream();

        _ = await stream.ReadAsync(ReadabilityCheckBuffer, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
    }

    public Stream GetContentStream()
    {
        return _stream ??= OpenContentStream();

        Stream OpenContentStream()
        {
            long fileId = 0;

            try
            {
                fileId = _file.ObjectId;

                return new SafeFileStream(_file.OpenRead(ownsHandle: false), fileId);
            }
            catch (Exception ex) when (ExceptionMapping.TryMapException(ex, fileId, out var mappedException))
            {
                throw mappedException;
            }
        }
    }

    public Task<ReadOnlyMemory<byte>> GetThumbnailAsync(int numberOfPixelsOnLargestSide, int maxNumberOfBytes, CancellationToken cancellationToken)
    {
        return _thumbnailGenerator.GenerateThumbnailAsync(_file.FullPath, numberOfPixelsOnLargestSide, maxNumberOfBytes, cancellationToken);
    }

    public Task<FileMetadata?> GetMetadataAsync()
    {
        return _fileMetadataGenerator.GetMetadataAsync(_file.FullPath);
    }

    public Task<IReadOnlySet<PhotoTag>> GetPhotoTagsAsync(CancellationToken cancellationToken)
    {
        return _photoTagsGenerator.GetPhotoTagsAsync(_file.FullPath, cancellationToken);
    }

    public bool TryGetFileHasChanged(out bool hasChanged)
    {
        try
        {
            _file.Refresh();

            hasChanged = _file.Size != Size || _file.LastWriteTimeUtc != LastWriteTimeUtc;
        }
        catch
        {
            // Assume that the file has changed if it could not be refreshed
            hasChanged = true;
        }

        return true;
    }

    public void Dispose()
    {
        _file.Dispose();
        _stream?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();

        return ValueTask.CompletedTask;
    }
}
