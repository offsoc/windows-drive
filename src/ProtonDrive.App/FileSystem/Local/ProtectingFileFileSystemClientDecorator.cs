using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.App.SystemIntegration;
using ProtonDrive.Shared;
using ProtonDrive.Shared.IO;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.FileSystem.Local;

internal sealed class ProtectingFileFileSystemClientDecorator : FileSystemClientDecoratorBase<long>
{
    private readonly ISyncFolderStructureProtector _folderStructureProtector;

    public ProtectingFileFileSystemClientDecorator(ISyncFolderStructureProtector folderStructureProtector, IFileSystemClient<long> decoratedInstance)
        : base(decoratedInstance)
    {
        _folderStructureProtector = folderStructureProtector;
    }

    public override async Task<IRevisionCreationProcess<long>> CreateFile(
        NodeInfo<long> info,
        string? tempFileName,
        IThumbnailProvider thumbnailProvider,
        IFileMetadataProvider fileMetadataProvider,
        Action<Progress>? progressCallback,
        CancellationToken cancellationToken)
    {
        var revisionCreationProcess = await base.CreateFile(info, tempFileName, thumbnailProvider, fileMetadataProvider, progressCallback, cancellationToken)
            .ConfigureAwait(false);

        return new ProtectingRevisionCreationProcess(this, revisionCreationProcess);
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

            return new ProtectingRevisionCreationProcess(this, revisionCreationProcess);
        }
        catch
        {
            ProtectFile(info);
            throw;
        }
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
        private readonly ProtectingFileFileSystemClientDecorator _fileProtector;
        private readonly IRevisionCreationProcess<long> _decoratedInstance;

        public ProtectingRevisionCreationProcess(ProtectingFileFileSystemClientDecorator fileProtector, IRevisionCreationProcess<long> decoratedInstance)
        {
            _fileProtector = fileProtector;
            _decoratedInstance = decoratedInstance;
        }

        public NodeInfo<long> FileInfo => _decoratedInstance.FileInfo;

        public NodeInfo<long> BackupInfo
        {
            get => _decoratedInstance.BackupInfo;
            set => _decoratedInstance.BackupInfo = value;
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

        public ValueTask DisposeAsync()
        {
            _fileProtector.ProtectFile(FileInfo);

            return _decoratedInstance.DisposeAsync();
        }
    }
}
