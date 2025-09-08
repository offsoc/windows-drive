using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Local;

internal class RootedFileSystemClientDecorator : FileSystemClientDecoratorBase<long>
{
    private readonly IRootDirectory<long> _rootDirectory;

    public RootedFileSystemClientDecorator(IRootDirectory<long> rootDirectory, IFileSystemClient<long> origin)
        : base(origin)
    {
        _rootDirectory = rootDirectory;

        if (IsDefault(_rootDirectory.Id))
        {
            throw new ArgumentException("Root folder identity value must be specified", nameof(rootDirectory));
        }
    }

    public override void Connect(string syncRootPath, IFileHydrationDemandHandler<long> fileHydrationDemandHandler)
    {
        var path = Path.Combine(_rootDirectory.Path, syncRootPath);

        base.Connect(path, fileHydrationDemandHandler);
    }

    public override async Task<NodeInfo<long>> GetInfo(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        if (!IsRoot(info))
        {
            return ToRelative(await base.GetInfo(ToAbsolute(info), cancellationToken).ConfigureAwait(false));
        }

        if (!string.IsNullOrEmpty(info.Path))
        {
            throw new ArgumentException($"The root folder path must be empty", nameof(info));
        }

        // The request about the root node always succeeds, the response is crafted from known data.
        return NodeInfo<long>.Directory()
            .WithId(_rootDirectory.Id)
            .WithName(string.Empty);
    }

    public override IAsyncEnumerable<NodeInfo<long>> Enumerate(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.Enumerate(ToAbsolute(info), cancellationToken).Select(ToRelative);

    public override Task<IRevision> OpenFileForReading(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.OpenFileForReading(ToAbsolute(info), cancellationToken);

    public override Task<NodeInfo<long>> CreateDirectory(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.CreateDirectory(ToAbsolute(info), cancellationToken);

    public override Task<IRevisionCreationProcess<long>> CreateFile(
        NodeInfo<long> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
        => base.CreateFile(ToAbsolute(info), tempFileName, thumbnailProvider, fileMetadataProvider, progressCallback, cancellationToken);

    public override async Task<IRevisionCreationProcess<long>> CreateRevision(
        NodeInfo<long> info,
        long size,
        DateTime lastWriteTime,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
        => new RootedFileWriteProcess(
            await base.CreateRevision(
                    ToAbsolute(info),
                    size,
                    lastWriteTime,
                    tempFileName,
                    thumbnailProvider,
                    fileMetadataProvider,
                    progressCallback,
                    cancellationToken)
                .ConfigureAwait(false),
            this);

    public override Task Move(NodeInfo<long> info, NodeInfo<long> newInfo, CancellationToken cancellationToken)
    {
        var nodeInfo = ToAbsolute(info);

        return base.Move(nodeInfo, ToAbsoluteDestination(nodeInfo, newInfo), cancellationToken);
    }

    public override Task Delete(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.Delete(ToAbsolute(info), cancellationToken);

    public override Task DeletePermanently(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.DeletePermanently(ToAbsolute(info), cancellationToken);

    public override Task DeleteRevision(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.DeleteRevision(ToAbsolute(info), cancellationToken);

    public override void SetInSyncState(NodeInfo<long> info)
        => base.SetInSyncState(ToAbsolute(info));

    public override Task HydrateFileAsync(NodeInfo<long> info, CancellationToken cancellationToken)
        => base.HydrateFileAsync(ToAbsolute(info), cancellationToken);

    private static bool IsDefault(long value)
    {
        return value.Equals(default);
    }

    private bool IsRoot(NodeInfo<long> info)
    {
        return (IsDefault(info.Id) && string.IsNullOrEmpty(info.Path)) ||
               (!IsDefault(info.Id) && info.Id.Equals(_rootDirectory.Id));
    }

    private NodeInfo<long> ToAbsolute(NodeInfo<long> nodeInfo)
    {
        var info = nodeInfo.Copy().WithPath(ToAbsolutePath(nodeInfo.Path));

        if (IsDefault(info.Id))
        {
            // Empty Path means it's the replica root directory
            if (string.IsNullOrEmpty(info.Path))
            {
                info = info.WithId(_rootDirectory.Id);
            }
        }

        if (IsDefault(info.ParentId) && !string.IsNullOrEmpty(info.Path))
        {
            // Not empty path without directory name means parent is the replica root directory
            if (string.IsNullOrEmpty(Path.GetDirectoryName(info.Path)))
            {
                info = info.WithParentId(_rootDirectory.Id);
            }
        }

        return info;
    }

    private NodeInfo<long> ToAbsoluteDestination(NodeInfo<long> nodeInfo, NodeInfo<long> destinationInfo)
    {
        var info = destinationInfo.Copy().WithPath(ToAbsoluteDestinationPath(destinationInfo.Path));

        // Destination cannot be the replica root directory, only the parent can be.
        if (IsDefault(info.ParentId))
        {
            // The destination is on the same parent as the source
            if (string.IsNullOrEmpty(info.Path))
            {
                info = info.WithParentId(nodeInfo.ParentId);
            }

            // Not empty path without directory name means parent is the replica root directory
            else if (!string.IsNullOrEmpty(info.Path) && string.IsNullOrEmpty(Path.GetDirectoryName(info.Path)))
            {
                info = info.WithParentId(_rootDirectory.Id);
            }
        }

        return info;
    }

    private NodeInfo<long> ToRelative(NodeInfo<long> nodeInfo)
    {
        return string.IsNullOrEmpty(nodeInfo.Path)
            ? nodeInfo
            : nodeInfo.Copy().WithPath(ToRelativePath(nodeInfo.Path));
    }

    private string ToAbsoluteDestinationPath(string path)
    {
        return !string.IsNullOrEmpty(path) ? ToAbsolutePath(path) : path;
    }

    private string ToAbsolutePath(string path)
    {
        return Path.Combine(_rootDirectory.Path, path);
    }

    private string ToRelativePath(string path)
    {
        var relativePath = Path.GetRelativePath(_rootDirectory.Path, path);

        return relativePath != path ? relativePath : string.Empty;
    }

    private class RootedFileWriteProcess : IRevisionCreationProcess<long>
    {
        private readonly IRevisionCreationProcess<long> _decoratedInstance;
        private readonly RootedFileSystemClientDecorator _converter;

        public RootedFileWriteProcess(IRevisionCreationProcess<long> instanceToDecorate, RootedFileSystemClientDecorator converter)
        {
            _decoratedInstance = instanceToDecorate;
            _converter = converter;
        }

        public NodeInfo<long> FileInfo => _decoratedInstance.FileInfo;

        public NodeInfo<long> BackupInfo
        {
            get => _converter.ToRelative(_decoratedInstance.BackupInfo);
            set => _decoratedInstance.BackupInfo = _converter.ToAbsolute(value);
        }

        public bool ImmediateHydrationRequired => _decoratedInstance.ImmediateHydrationRequired;

        public Stream OpenContentStream()
        {
            return _decoratedInstance.OpenContentStream();
        }

        public Task<NodeInfo<long>> FinishAsync(CancellationToken cancellationToken)
        {
            return _decoratedInstance.FinishAsync(cancellationToken);
        }

        public ValueTask DisposeAsync() => _decoratedInstance.DisposeAsync();
    }
}
