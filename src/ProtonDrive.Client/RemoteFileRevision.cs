using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client;

internal sealed class RemoteFileRevision : IRevision
{
    private readonly Stream _contentStream;
    private readonly ExtendedAttributes? _extendedAttributes;

    public RemoteFileRevision(Stream contentStream, DateTime creationTimeUtc, DateTime lastWriteTimeUtc, ExtendedAttributes? extendedAttributes)
    {
        _contentStream = contentStream;
        _extendedAttributes = extendedAttributes;
        CreationTimeUtc = creationTimeUtc;
        LastWriteTimeUtc = lastWriteTimeUtc;
    }

    public long Size => _contentStream.Length;
    public DateTime CreationTimeUtc { get; }
    public DateTime LastWriteTimeUtc { get; }

    public Task CheckReadabilityAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Stream GetContentStream()
    {
        return _contentStream;
    }

    public bool TryGetFileHasChanged(out bool hasChanged)
    {
        hasChanged = false;
        return false;
    }

    public Task<ReadOnlyMemory<byte>> GetThumbnailAsync(int numberOfPixelsOnLargestSide, int maxNumberOfBytes, CancellationToken cancellationToken)
    {
        return Task.FromResult(ReadOnlyMemory<byte>.Empty);
    }

    public Task<FileMetadata?> GetMetadataAsync()
    {
        if (_extendedAttributes is null)
        {
            return Task.FromResult(default(FileMetadata?));
        }

        var metadata = FileMetadataSanitizer.GetFileMetadata(
            _extendedAttributes.Media?.Width,
            _extendedAttributes.Media?.Height,
            _extendedAttributes.Media?.Duration,
            _extendedAttributes.Camera?.Orientation,
            _extendedAttributes.Camera?.Device,
            _extendedAttributes.Camera?.CaptureTime,
            _extendedAttributes.Location?.Latitude,
            _extendedAttributes.Location?.Longitude);

        return Task.FromResult(metadata);
    }

    public Task<IReadOnlySet<PhotoTag>> GetPhotoTagsAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public void Dispose()
    {
        _contentStream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _contentStream.DisposeAsync();
    }
}
