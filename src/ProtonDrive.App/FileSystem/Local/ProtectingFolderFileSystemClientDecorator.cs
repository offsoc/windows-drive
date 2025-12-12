using ProtonDrive.App.SystemIntegration;
using ProtonDrive.Shared;
using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Local;

internal sealed class ProtectingFolderFileSystemClientDecorator : FileSystemClientDecoratorBase<long>
{
    private readonly ISyncFolderStructureProtector _folderStructureProtector;
    private readonly ConcurrentFolderStructureProtector<long> _concurrentFolderStructureProtector;

    public ProtectingFolderFileSystemClientDecorator(ISyncFolderStructureProtector folderStructureProtector, IFileSystemClient<long> decoratedInstance)
        : base(decoratedInstance)
    {
        _folderStructureProtector = folderStructureProtector;

        _concurrentFolderStructureProtector = new ConcurrentFolderStructureProtector<long>(_folderStructureProtector);
    }

    public override async Task<NodeInfo<long>> CreateDirectory(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        await using ((await UnprotectParentFolderAsync(info, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
        {
            var resultInfo = await base.CreateDirectory(info, cancellationToken).ConfigureAwait(false);

            // CreateDirectory does not fill the Path, therefore we cannot use resultInfo for protecting folder
            ProtectFolder(info);

            return resultInfo;
        }
    }

    public override async Task<IRevisionCreationProcess<long>> CreateFile(
        NodeInfo<long> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        var parentFolderProtectionHolder = await UnprotectParentFolderAsync(info, cancellationToken).ConfigureAwait(false);

        try
        {
            var revisionCreationProcess = await base.CreateFile(
                info,
                tempFileName,
                thumbnailProvider,
                fileMetadataProvider,
                progressCallback,
                cancellationToken).ConfigureAwait(false);

            return new ProtectingRevisionCreationProcess(this, revisionCreationProcess, parentFolderProtectionHolder);
        }
        catch
        {
            await parentFolderProtectionHolder.DisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    public override async Task<IRevisionCreationProcess<long>> CreateRevision(
        NodeInfo<long> info,
        long size,
        DateTime lastWriteTime,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        var parentFolderProtectionHolder = await UnprotectParentFolderAsync(info, cancellationToken).ConfigureAwait(false);

        try
        {
            UnprotectFile(info);

            try
            {
                var revisionCreationProcess = await base.CreateRevision(
                    info,
                    size,
                    lastWriteTime,
                    tempFileName,
                    thumbnailProvider,
                    fileMetadataProvider,
                    progressCallback,
                    cancellationToken).ConfigureAwait(false);

                return new ProtectingRevisionCreationProcess(this, revisionCreationProcess, parentFolderProtectionHolder);
            }
            catch
            {
                ProtectFile(info);
                throw;
            }
        }
        catch
        {
            await parentFolderProtectionHolder.DisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    public override async Task Move(NodeInfo<long> info, NodeInfo<long> newInfo, CancellationToken cancellationToken)
    {
        await using ((await UnprotectParentFolderAsync(info, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
        await using ((await UnprotectParentFolderAsync(newInfo, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
        {
            await base.Move(info, newInfo, cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task Delete(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        await using ((await UnprotectParentFolderAsync(info, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
        {
            UnprotectFileOrBranch(info);

            try
            {
                await base.Delete(info, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // In case of failure, we do not add branch protection
                ProtectFileOrFolder(info);

                throw;
            }
        }
    }

    public override async Task DeletePermanently(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        await using ((await UnprotectParentFolderAsync(info, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
        {
            UnprotectFileOrBranch(info);

            try
            {
                await base.DeletePermanently(info, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // In case of failure, we do not add branch protection
                ProtectFileOrFolder(info);

                throw;
            }
        }
    }

    private Task<IAsyncDisposable> UnprotectParentFolderAsync(NodeInfo<long> info, CancellationToken cancellationToken)
    {
        var parentPath = info.GetParentFolderPath();

        if (string.IsNullOrEmpty(parentPath) || info.ParentId == default)
        {
            return Task.FromResult(AsyncDisposable.Empty);
        }

        return _concurrentFolderStructureProtector.UnprotectFolderAsync(info.ParentId, parentPath, cancellationToken);
    }

    private void ProtectFileOrFolder(NodeInfo<long> info)
    {
        if (info.IsFile())
        {
            ProtectFile(info);
        }
        else
        {
            ProtectFolder(info);
        }
    }

    private void UnprotectFileOrBranch(NodeInfo<long> info)
    {
        if (info.IsFile())
        {
            UnprotectFile(info);
        }
        else
        {
            UnprotectBranch(info);
        }
    }

    private void ProtectFolder(NodeInfo<long> info)
    {
        Ensure.IsTrue(info.IsDirectory(), "Must be a folder", nameof(info));

        _folderStructureProtector.ProtectFolder(info.Path, FolderProtectionType.ReadOnly);
    }

    private void UnprotectBranch(NodeInfo<long> info)
    {
        Ensure.IsTrue(info.IsDirectory(), "Must be a folder", nameof(info));

        _folderStructureProtector.UnprotectBranch(info.Path, FolderProtectionType.ReadOnly, FileProtectionType.ReadOnly);
    }

    private void ProtectFile(NodeInfo<long> info)
    {
        Ensure.IsTrue(info.IsFile(), "Must be a file", nameof(info));

        _folderStructureProtector.ProtectFile(info.Path, FileProtectionType.ReadOnly);
    }

    private void UnprotectFile(NodeInfo<long> info)
    {
        Ensure.IsTrue(info.IsFile(), "Must be a file", nameof(info));

        _folderStructureProtector.UnprotectFile(info.Path, FileProtectionType.ReadOnly);
    }

    private sealed class ProtectingRevisionCreationProcess : IRevisionCreationProcess<long>
    {
        private readonly ProtectingFolderFileSystemClientDecorator _fileProtector;
        private readonly IRevisionCreationProcess<long> _decoratedInstance;
        private readonly IAsyncDisposable _parentFolderProtectionHolder;

        public ProtectingRevisionCreationProcess(
            ProtectingFolderFileSystemClientDecorator fileProtector,
            IRevisionCreationProcess<long> decoratedInstance,
            IAsyncDisposable parentFolderProtectionHolder)
        {
            _fileProtector = fileProtector;
            _decoratedInstance = decoratedInstance;
            _parentFolderProtectionHolder = parentFolderProtectionHolder;
        }

        public NodeInfo<long> FileInfo => _decoratedInstance.FileInfo;

        public NodeInfo<long> BackupInfo
        {
            get => _decoratedInstance.BackupInfo;
            set => _decoratedInstance.BackupInfo = value;
        }

        public bool ImmediateHydrationRequired => _decoratedInstance.ImmediateHydrationRequired;
        public bool CanGetContentStream => _decoratedInstance.CanGetContentStream;

        public Stream GetContentStream()
        {
            return _decoratedInstance.GetContentStream();
        }

        public Task WriteContentAsync(Stream source, CancellationToken cancellationToken)
        {
            return _decoratedInstance.WriteContentAsync(source, cancellationToken);
        }

        public Task<NodeInfo<long>> FinishAsync(CancellationToken cancellationToken)
        {
            return _decoratedInstance.FinishAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _parentFolderProtectionHolder.DisposeAsync().ConfigureAwait(false);
            _fileProtector.ProtectFile(FileInfo);

            await _decoratedInstance.DisposeAsync().ConfigureAwait(false);
        }
    }
}
