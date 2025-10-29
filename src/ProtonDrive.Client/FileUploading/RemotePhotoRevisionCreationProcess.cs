using ProtonDrive.Client.Contracts;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal sealed class RemotePhotoRevisionCreationProcess : RemoteRevisionCreationProcess
{
    private readonly DateTime _creationTimeUtc;
    private readonly DateTime _lastWriteTimeUtc;

    public RemotePhotoRevisionCreationProcess(
        NodeInfo<string> fileInfo,
        Stream contentStream,
        IReadOnlyCollection<UploadedBlock> uploadedBlocks,
        int blockSize,
        DateTime creationTimeUtc,
        DateTime lastWriteTimeUtc,
        IRevisionSealer revisionSealer)
        : base(fileInfo, contentStream, uploadedBlocks, blockSize, revisionSealer)
    {
        _creationTimeUtc = creationTimeUtc;
        _lastWriteTimeUtc = lastWriteTimeUtc;
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
}
