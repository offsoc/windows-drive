namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IRevision : IThumbnailProvider, IFileMetadataProvider, IDisposable, IAsyncDisposable
{
    long Size { get; }
    bool CanGetContentStream { get; }

    Stream GetContentStream();
    Task CheckReadabilityAsync(CancellationToken cancellationToken);
    Task CopyContentToAsync(Stream destination, CancellationToken cancellationToken);
    bool TryGetFileHasChanged(out bool hasChanged);
}
