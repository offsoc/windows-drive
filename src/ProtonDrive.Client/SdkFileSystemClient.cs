using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client;

/// <summary>
/// This client uses the Proton Drive SDK to access the remote file system.
/// This client allows the hybrid client to be created even when the SDK implementation is not yet fully available.
/// </summary>
internal sealed class SdkFileSystemClient : IFileSystemClient<string>
{
    private readonly FileSystemClientParameters _parameters;

    public SdkFileSystemClient(FileSystemClientParameters parameters)
    {
        _parameters = parameters;
    }

    public void Connect(string syncRootPath, IFileHydrationDemandHandler<string> fileHydrationDemandHandler)
    {
        // Do nothing
    }

    public Task DisconnectAsync()
    {
        return Task.CompletedTask;
    }

    public Task<NodeInfo<string>> GetInfo(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public IAsyncEnumerable<NodeInfo<string>> Enumerate(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task<NodeInfo<string>> CreateDirectory(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task<IRevisionCreationProcess<string>> CreateFile(
        NodeInfo<string> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task<IRevision> OpenFileForReading(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task<IRevisionCreationProcess<string>> CreateRevision(
        NodeInfo<string> info,
        long size,
        DateTime lastWriteTime,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task MoveAsync(IReadOnlyList<NodeInfo<string>> sourceNodes, NodeInfo<string> destinationInfo, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task Move(NodeInfo<string> info, NodeInfo<string> destinationInfo, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task Delete(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task DeletePermanently(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task DeleteRevision(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public void SetInSyncState(NodeInfo<string> info)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }

    public Task HydrateFileAsync(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("SDK client implementation is not yet available");
    }
}
