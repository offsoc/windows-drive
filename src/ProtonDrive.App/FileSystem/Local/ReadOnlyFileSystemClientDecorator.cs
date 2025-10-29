using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Local;

internal class ReadOnlyFileSystemClientDecorator : FileSystemClientDecoratorBase<long>
{
    public ReadOnlyFileSystemClientDecorator(IFileSystemClient<long> decoratedInstance)
        : base(decoratedInstance)
    {
    }

    public override Task<IRevisionCreationProcess<long>> CreateFile(
        NodeInfo<long> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        var readOnlyInfo = ToReadOnly(info);

        return base.CreateFile(readOnlyInfo, tempFileName, thumbnailProvider, fileMetadataProvider, progressCallback, cancellationToken);
    }

    public override Task<IRevisionCreationProcess<long>> CreateRevision(
        NodeInfo<long> info,
        long size,
        DateTime lastWriteTime,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        var readOnlyInfo = ToReadOnly(info);

        return base.CreateRevision(
            readOnlyInfo,
            size,
            lastWriteTime,
            tempFileName,
            thumbnailProvider,
            fileMetadataProvider,
            progressCallback,
            cancellationToken);
    }

    public override Task Delete(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        var readOnlyInfo = ToReadOnly(info);

        return base.Delete(readOnlyInfo, cancellationToken);
    }

    public override Task DeletePermanently(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        var readOnlyInfo = ToReadOnly(info);

        return base.DeletePermanently(readOnlyInfo, cancellationToken);
    }

    private NodeInfo<long> ToReadOnly(NodeInfo<long> nodeInfo)
    {
        if (nodeInfo.IsDirectory())
        {
            return nodeInfo;
        }

        return nodeInfo.Copy().WithAttributes(nodeInfo.Attributes | FileAttributes.ReadOnly);
    }
}
