namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IRevision : IThumbnailProvider, IFileMetadataProvider, IDisposable, IAsyncDisposable
{
    long Size { get; }

    Task CheckReadabilityAsync(CancellationToken cancellationToken);
    Stream GetContentStream();
    bool TryGetFileHasChanged(out bool hasChanged);
}
