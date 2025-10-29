using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Remote;

internal sealed class ReadOnlyFileSystemClientDecorator : FileSystemClientDecoratorBase<string>
{
    public ReadOnlyFileSystemClientDecorator(IFileSystemClient<string> instanceToDecorate)
        : base(instanceToDecorate)
    {
    }

    public override Task<NodeInfo<string>> CreateDirectory(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task<IRevisionCreationProcess<string>> CreateFile(
        NodeInfo<string> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task<IRevisionCreationProcess<string>> CreateRevision(
        NodeInfo<string> info,
        long size,
        DateTime lastWriteTime,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task Move(NodeInfo<string> info, NodeInfo<string> destinationInfo, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task Delete(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task DeletePermanently(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task DeleteRevision(NodeInfo<string> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    private static FileSystemClientException GetException()
    {
        return new FileSystemClientException(string.Empty, FileSystemErrorCode.ReadOnlyRoot);
    }
}
