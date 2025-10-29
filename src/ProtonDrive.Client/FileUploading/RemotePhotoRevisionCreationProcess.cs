using Microsoft.Extensions.Logging;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal sealed class RemotePhotoRevisionCreationProcess : RemoteRevisionCreationProcess
{
    private readonly DateTime _creationTimeUtc;
    private readonly DateTime _lastWriteTimeUtc;
    private readonly ILogger<RemotePhotoRevisionCreationProcess> _logger;

    public RemotePhotoRevisionCreationProcess(
        NodeInfo<string> fileInfo,
        Stream contentStream,
        IReadOnlyCollection<UploadedBlock> uploadedBlocks,
        int blockSize,
        DateTime creationTimeUtc,
        DateTime lastWriteTimeUtc,
        IRevisionSealer revisionSealer,
        ILogger<RemotePhotoRevisionCreationProcess> logger)
        : base(fileInfo, contentStream, uploadedBlocks, blockSize, revisionSealer)
    {
        _creationTimeUtc = creationTimeUtc;
        _lastWriteTimeUtc = lastWriteTimeUtc;
        _logger = logger;
    }

    protected override RevisionSealingParameters GetRevisionSealingParameters()
    {
        var revisionSealingParameters = base.GetRevisionSealingParameters();

        var defaultCaptureTimeUtc = _creationTimeUtc < _lastWriteTimeUtc ? _creationTimeUtc : _lastWriteTimeUtc;

        return new PhotoRevisionSealingParameters
        {
            Blocks = revisionSealingParameters.Blocks,
            Sha1Digest = revisionSealingParameters.Sha1Digest,
            DefaultCaptureTimeUtc = defaultCaptureTimeUtc,
            MainPhotoLinkId = FileInfo.MainPhotoLinkId,
        };
    }

    protected override async Task CopyFileContentAsync(Stream destination, Stream source, CancellationToken cancellationToken)
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
