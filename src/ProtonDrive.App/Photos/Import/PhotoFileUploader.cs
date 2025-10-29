using Microsoft.Extensions.Logging;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.App.Photos.Import;

internal sealed class PhotoFileUploader : IPhotoFileUploader
{
    private readonly IPhotoFileSystemClient<long> _localFileSystemClient;
    private readonly IFileSystemClient<string> _remoteFileSystemClient;
    private readonly ILogger<PhotoFileUploader> _logger;

    public PhotoFileUploader(
        IPhotoFileSystemClient<long> localFileSystemClient,
        IFileSystemClient<string> remoteFileSystemClient,
        ILogger<PhotoFileUploader> logger)
    {
        _localFileSystemClient = localFileSystemClient;
        _remoteFileSystemClient = remoteFileSystemClient;
        _logger = logger;
    }

    public async Task<NodeInfo<string>> UploadFileAsync(string filePath, string parentLinkId, string? mainPhotoLinkId, CancellationToken cancellationToken)
    {
        var nodeInfo = NodeInfo<long>.File().WithPath(filePath);
        var sourceRevision = await _localFileSystemClient.OpenFileForReading(nodeInfo, cancellationToken).ConfigureAwait(false);

        await using (sourceRevision.ConfigureAwait(false))
        {
            var remoteNodeInfo = NodeInfo<string>.File()
                .WithParentId(parentLinkId)
                .WithMainPhotoLinkId(mainPhotoLinkId)
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

    private async Task<NodeInfo<string>> FinishRevisionCreationAsync(
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

    private async Task CopyFileContentAsync(Stream destination, Stream source, CancellationToken cancellationToken)
    {
        // The Drive encrypted file write stream requires the Length to be set before copying the content.
        // The Drive encrypted file read stream can report Length value different from the length of the unencrypted data.
        destination.SetLength(source.Length);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        // Set the Length to the real number of bytes copied.
        if (destination.Position != destination.Length)
        {
            _logger.LogWarning(
                "File size changed while being uploaded: " +
                "destination position {Position:N0} differs from destination length {Length:N0} " +
                "(source position {SourcePosition:N0}, current source length {SourceLength:N0})",
                destination.Position,
                destination.Length,
                source.Position,
                source.Length);

            throw new PhotoFileSizeMismatchException(
                "Failed to import file due to file size mismatch: " +
                $"source {source.Length:N0} bytes, expected {destination.Length:N0} bytes, got {destination.Position:N0} bytes.");
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
