using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtonDrive.Shared.IO;
using ProtonDrive.Shared.Logging;
using ProtonDrive.Shared.Threading;
using ProtonDrive.Sync.Adapter.OnDemandHydration.FileSizeCorrection;
using ProtonDrive.Sync.Adapter.Shared;
using ProtonDrive.Sync.Adapter.Trees.Adapter;
using ProtonDrive.Sync.Shared.Adapters;
using ProtonDrive.Sync.Shared.FileSystem;
using ProtonDrive.Sync.Shared.SyncActivity;
using ProtonDrive.Sync.Shared.Trees.FileSystem;

namespace ProtonDrive.Sync.Adapter.OnDemandHydration;

internal sealed class HydrationDemandHandler<TId, TAltId> : IFileHydrationDemandHandler<TAltId>
    where TId : struct, IEquatable<TId>
    where TAltId : IEquatable<TAltId>
{
    private readonly ILogger<HydrationDemandHandler<TId, TAltId>> _logger;
    private readonly IScheduler _executionScheduler;
    private readonly IScheduler _syncScheduler;
    private readonly AdapterTree<TId, TAltId> _adapterTree;
    private readonly IFileRevisionProvider<TId> _fileRevisionProvider;
    private readonly IMappedNodeIdentityProvider<TId> _mappedNodeIdProvider;
    private readonly IFileSizeCorrector<TId, TAltId> _fileSizeCorrector;
    private readonly SyncActivity<TId> _syncActivity;

    public HydrationDemandHandler(
        ILogger<HydrationDemandHandler<TId, TAltId>> logger,
        IScheduler executionScheduler,
        IScheduler syncScheduler,
        AdapterTree<TId, TAltId> adapterTree,
        IFileRevisionProvider<TId> fileRevisionProvider,
        IMappedNodeIdentityProvider<TId> mappedNodeIdProvider,
        IFileSizeCorrector<TId, TAltId> fileSizeCorrector,
        SyncActivity<TId> syncActivity)
    {
        _logger = logger;
        _executionScheduler = executionScheduler;
        _syncScheduler = syncScheduler;
        _adapterTree = adapterTree;
        _fileRevisionProvider = fileRevisionProvider;
        _mappedNodeIdProvider = mappedNodeIdProvider;
        _fileSizeCorrector = fileSizeCorrector;
        _syncActivity = syncActivity;
    }

    public async Task HandleAsync(IFileHydrationDemand<TAltId> hydrationDemand, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileNameToLog = _logger.GetSensitiveValueForLogging(hydrationDemand.FileInfo.Name);
        LogRequest();

        var nodeModel = await Schedule(() => Prepare(hydrationDemand), cancellationToken).ConfigureAwait(false);

        var syncActivityItem = nodeModel.GetSyncActivityItemForFileHydration(hydrationDemand.FileInfo);

        try
        {
            _syncActivity.OnProgress(syncActivityItem, Progress.Zero);

            var sourceRevision = await OpenFileForReadingAsync(nodeModel, cancellationToken).ConfigureAwait(false);

            syncActivityItem = syncActivityItem with
            {
                Stage = SyncActivityStage.Execution,
            };

            _syncActivity.OnProgress(syncActivityItem, Progress.Zero);

            var destinationContent = new ProgressReportingStream(hydrationDemand.HydrationStream, NotifyProgressChanged);

            await using (destinationContent.ConfigureAwait(false))
            {
                var initialLength = destinationContent.Length;

                await HydrateFileAsync(destinationContent, sourceRevision, cancellationToken).ConfigureAwait(false);

                var sizeMismatch = destinationContent.Length - initialLength;
                if (sizeMismatch != 0)
                {
                    LogSizeMismatch(nodeModel.Id, sizeMismatch);

                    await ScheduleExecution(() => CorrectFileSize(nodeModel, hydrationDemand, cancellationToken), cancellationToken).ConfigureAwait(false);
                }
            }

            _syncActivity.OnChanged(syncActivityItem, SyncActivityItemStatus.Succeeded);

            LogSuccess(nodeModel.Id);
        }
        catch (Exception ex)
        {
            var (errorCode, errorMessage) = ex.GetErrorInfo();

            switch (errorCode)
            {
                case FileSystemErrorCode.SharingViolation:
                    _syncActivity.OnWarning(syncActivityItem, errorCode, errorMessage);
                    break;

                case FileSystemErrorCode.Cancelled or FileSystemErrorCode.TransferAbortedDueToFileChange:
                    _syncActivity.OnCancelled(syncActivityItem, errorCode);
                    break;

                default:
                    _syncActivity.OnFailed(syncActivityItem, errorCode, errorMessage);
                    break;
            }

            if (ex is not OperationCanceledException)
            {
                throw;
            }

            LogCancellation();
        }

        return;

        void NotifyProgressChanged(Progress value)
        {
            _syncActivity.OnProgress(syncActivityItem, value);
        }

        void LogRequest()
        {
            _logger.LogInformation(
                "Requested on-demand hydration of \"{FileName}\" with external Id={ExternalId}",
                fileNameToLog,
                hydrationDemand.FileInfo.GetCompoundId());
        }

        void LogSuccess(TId nodeId)
        {
            _logger.LogInformation(
                "On-demand hydration of \"{FileName}\" with Id=\"{Root}\"/{Id} {ExternalId} succeeded",
                fileNameToLog,
                nodeId,
                hydrationDemand.FileInfo.Root?.Id,
                hydrationDemand.FileInfo.GetCompoundId());
        }

        void LogSizeMismatch(TId nodeId, long mismatch)
        {
            _logger.LogInformation(
                "On-demand hydration of \"{FileName}\" with Id=\"{Root}\"/{Id} {ExternalId} requires size correction by {Mismatch}",
                fileNameToLog,
                hydrationDemand.FileInfo.Root?.Id,
                nodeId,
                hydrationDemand.FileInfo.GetCompoundId(),
                mismatch);
        }

        void LogCancellation()
        {
            _logger.LogInformation(
                "On-demand hydration of \"{FileName}\" with external Id={ExternalId} was cancelled",
                fileNameToLog,
                hydrationDemand.FileInfo.GetCompoundId());
        }
    }

    private static bool ContentHasDiverged(AdapterTreeNode<TId, TAltId> node, NodeInfo<TAltId> nodeInfo)
    {
        return node.Model.Size != nodeInfo.Size || node.Model.LastWriteTime != nodeInfo.LastWriteTimeUtc;
    }

    private AdapterTreeNodeModel<TId, TAltId> Prepare(IFileHydrationDemand<TAltId> hydrationDemand)
    {
        var altId = hydrationDemand.FileInfo.GetCompoundId();

        if (altId.IsDefault())
        {
            throw new InvalidOperationException($"No identifier given for file \"{_logger.GetSensitiveValueForLogging(hydrationDemand.FileInfo.Name)}\"");
        }

        var node = _adapterTree.NodeByAltIdOrDefault(altId)
                   ?? throw new HydrationException($"Adapter Tree node with AltId={altId} does not exist");

        if (node.Type != NodeType.File)
        {
            throw new HydrationException($"Adapter Tree node with Id={node.Id} {altId} is not a file");
        }

        if (node.IsNodeOrBranchDeleted())
        {
            throw new HydrationException($"Adapter Tree node with Id={node.Id} {altId} or branch is deleted");
        }

        if (ContentHasDiverged(node, hydrationDemand.FileInfo))
        {
            throw new HydrationException($"File with node Id={node.Id} {altId} content has diverged");
        }

        return node.Model;
    }

    private Task CorrectFileSize(AdapterTreeNodeModel<TId, TAltId> nodeModel, IFileHydrationDemand<TAltId> hydrationDemand, CancellationToken cancellationToken)
    {
        return _fileSizeCorrector.UpdateSizeAsync(nodeModel, hydrationDemand, cancellationToken);
    }

    private async Task<IRevision> OpenFileForReadingAsync(AdapterTreeNodeModel<TId, TAltId> nodeModel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mappedNodeId = await _mappedNodeIdProvider.GetMappedNodeIdOrDefaultAsync(nodeModel.Id, cancellationToken).ConfigureAwait(false);
        if (mappedNodeId is null)
        {
            throw new HydrationException(
                $"File with Adapter Tree node Id={nodeModel.Id} is not mapped",
                new FileSystemClientException(string.Empty, FileSystemErrorCode.ObjectNotFound));
        }

        return await _fileRevisionProvider.OpenFileForReadingAsync(mappedNodeId.Value, nodeModel.ContentVersion, cancellationToken).ConfigureAwait(false);
    }

    private async Task HydrateFileAsync(Stream destinationContent, IRevision sourceRevision, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourceContent = sourceRevision.GetContentStream();
        await using (sourceContent.ConfigureAwait(false))
        {
            await sourceContent.CopyToAsync(destinationContent, cancellationToken).ConfigureAwait(false);

            if (destinationContent.Position < destinationContent.Length)
            {
                destinationContent.SetLength(destinationContent.Position);
            }
        }
    }

    [DebuggerHidden]
    [DebuggerStepThrough]
    private Task ScheduleExecution(Func<Task> origin, CancellationToken cancellationToken)
    {
        return _executionScheduler.Schedule(origin, cancellationToken);
    }

    [DebuggerHidden]
    [DebuggerStepThrough]
    private Task<T> Schedule<T>(Func<T> origin, CancellationToken cancellationToken)
    {
        return _syncScheduler.Schedule(origin, cancellationToken);
    }
}
