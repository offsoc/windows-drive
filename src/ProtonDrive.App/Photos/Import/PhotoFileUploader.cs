using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoFileUploader : IPhotoFileUploader
{
    private readonly IPhotoFileSystemClient<long> _localFileSystemClient;
    private readonly IFileSystemClient<string> _remoteFileSystemClient;

    public PhotoFileUploader(IPhotoFileSystemClient<long> localFileSystemClient, IFileSystemClient<string> remoteFileSystemClient)
    {
        _localFileSystemClient = localFileSystemClient;
        _remoteFileSystemClient = remoteFileSystemClient;
    }

    public async Task<NodeInfo<string>> UploadFileAsync(string filePath, string parentLinkId, CancellationToken cancellationToken)
    {
        var nodeInfo = NodeInfo<long>.File().WithPath(filePath);
        var sourceRevision = await _localFileSystemClient.OpenFileForReading(nodeInfo, cancellationToken).ConfigureAwait(false);

        await using (sourceRevision.ConfigureAwait(false))
        {
            var remoteNodeInfo = NodeInfo<string>.File()
                .WithParentId(parentLinkId)
                .WithPath(filePath)
                .WithName(Path.GetFileName(filePath))
                .WithSize(sourceRevision.Size)
                .WithLastWriteTimeUtc(sourceRevision.LastWriteTimeUtc);

            var destinationRevision = await _remoteFileSystemClient.CreateFile(
                remoteNodeInfo,
                tempFileName: null,
                sourceRevision,
                sourceRevision,
                progressCallback: null,
                cancellationToken).ConfigureAwait(false);

            await using (destinationRevision.ConfigureAwait(false))
            {
                var result = await FinishRevisionCreationAsync(sourceRevision, destinationRevision, cancellationToken).ConfigureAwait(false);
                return result;
            }
        }
    }

    private static async Task<NodeInfo<string>> FinishRevisionCreationAsync(
        IRevision sourceRevision,
        IRevisionCreationProcess<string> destinationRevision,
        CancellationToken cancellationToken)
    {
        var destinationContent = destinationRevision.OpenContentStream();

        await using (destinationContent.ConfigureAwait(false))
        {
            var sourceContent = sourceRevision.GetContentStream();
            await using (sourceContent.ConfigureAwait(false))
            {
                await CopyFileContentAsync(destinationContent, sourceContent, cancellationToken).ConfigureAwait(false);
            }

            return await destinationRevision.FinishAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task CopyFileContentAsync(Stream destination, Stream source, CancellationToken cancellationToken)
    {
        // The Drive encrypted file write stream requires the Length to be set before copying the content.
        // The Drive encrypted file read stream can report Length value different from the length of the unencrypted data.
        destination.SetLength(source.Length);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        // Set the Length to the real number of bytes copied.
        if (destination.Position != destination.Length)
        {
            destination.SetLength(destination.Position);
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
