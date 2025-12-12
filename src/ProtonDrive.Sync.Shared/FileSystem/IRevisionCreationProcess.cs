namespace ProtonDrive.Sync.Shared.FileSystem;

public interface IRevisionCreationProcess<TId> : IAsyncDisposable
    where TId : IEquatable<TId>
{
    NodeInfo<TId> FileInfo { get; }
    NodeInfo<TId> BackupInfo { get; set; }
    bool ImmediateHydrationRequired { get; }
    bool CanGetContentStream { get; }

    Stream GetContentStream();
    Task WriteContentAsync(Stream source, CancellationToken cancellationToken);
    Task<NodeInfo<TId>> FinishAsync(CancellationToken cancellationToken);
}
