using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Local;

internal sealed class VirtualFileRootFileSystemClientDecorator : FileSystemClientDecoratorBase<long>
{
    private readonly long _parentFolderId;
    private readonly string _rootFileName;

    public VirtualFileRootFileSystemClientDecorator(long parentFolderId, string rootFileName, IFileSystemClient<long> instanceToDecorate)
        : base(instanceToDecorate)
    {
        _parentFolderId = parentFolderId;
        _rootFileName = rootFileName;
    }

    public override void Connect(string syncRootPath, IFileHydrationDemandHandler<long> fileHydrationDemandHandler)
    {
        base.Connect(_rootFileName, fileHydrationDemandHandler);
    }

    public override Task<NodeInfo<long>> GetInfo(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        ValidateFile(info);

        return base.GetInfo(info, cancellationToken);
    }

    public override IAsyncEnumerable<NodeInfo<long>> Enumerate(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        if (!IsRoot(info))
        {
            throw new FileSystemClientException("Unexpected folder in the file root", FileSystemErrorCode.ObjectNotFound);
        }

        return base.Enumerate(info, cancellationToken).Where(x => x.IsFile() && x.Name.Equals(_rootFileName, StringComparison.Ordinal));
    }

    public override Task<NodeInfo<long>> CreateDirectory(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task<IRevisionCreationProcess<long>> CreateFile(
        NodeInfo<long> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        ValidateFile(info);

        return base.CreateFile(info, tempFileName, thumbnailProvider, fileMetadataProvider, progressCallback, cancellationToken);
    }

    public override Task<IRevision> OpenFileForReading(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        ValidateFile(info);

        return base.OpenFileForReading(info, cancellationToken);
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
        ValidateFile(info);

        return base.CreateRevision(info, size, lastWriteTime, tempFileName, thumbnailProvider, fileMetadataProvider, progressCallback, cancellationToken);
    }

    public override Task Move(NodeInfo<long> info, NodeInfo<long> destinationInfo, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task Delete(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task DeletePermanently(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override Task DeleteRevision(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        throw GetException();
    }

    public override void SetInSyncState(NodeInfo<long> info)
    {
        ValidateFile(info);

        base.SetInSyncState(info);
    }

    public override Task HydrateFileAsync(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        ValidateFile(info);

        return base.HydrateFileAsync(info, cancellationToken);
    }

    private static bool IsDefault(long value) => value.Equals(default);

    private bool IsRoot(NodeInfo<long> info)
    {
        return (IsDefault(info.Id) && string.IsNullOrEmpty(info.Path)) ||
            (!IsDefault(info.Id) && info.Id.Equals(_parentFolderId));
    }

    private void ValidateFile(NodeInfo<long> info)
    {
        if (!IsDefault(info.ParentId) && !info.ParentId.Equals(_parentFolderId))
        {
            throw new FileSystemClientException($"Unexpected ParentId={info.ParentId}", FileSystemErrorCode.ObjectNotFound);
        }

        if (!string.Equals(info.Name, _rootFileName, StringComparison.Ordinal))
        {
            throw new FileSystemClientException("Unexpected file name", FileSystemErrorCode.PathNotFound);
        }
    }

    private static Exception GetException()
    {
        return new FileSystemClientException("Operation not supported on the virtual file root", FileSystemErrorCode.Unknown);
    }
}
