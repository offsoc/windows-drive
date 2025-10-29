using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client;

public sealed class SdkRemoteFileSystemClient : IFileSystemClient<string>
{
    public void Connect(string syncRootPath, IFileHydrationDemandHandler<string> fileHydrationDemandHandler)
    {
        throw new NotSupportedException();
    }

    public Task DisconnectAsync()
    {
        throw new NotSupportedException();
    }

    public Task<NodeInfo<string>> GetInfo(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public IAsyncEnumerable<NodeInfo<string>> Enumerate(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<NodeInfo<string>> CreateDirectory(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IRevisionCreationProcess<string>> CreateFile(
        NodeInfo<string> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IRevision> OpenFileForReading(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
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
        throw new NotSupportedException();
    }

    public Task Move(NodeInfo<string> info, NodeInfo<string> destinationInfo, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MoveAsync(IReadOnlyList<NodeInfo<string>> sourceNodes, NodeInfo<string> destinationInfo, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task Delete(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task DeletePermanently(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task DeleteRevision(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public void SetInSyncState(NodeInfo<string> info)
    {
        throw new NotSupportedException();
    }

    public Task HydrateFileAsync(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
