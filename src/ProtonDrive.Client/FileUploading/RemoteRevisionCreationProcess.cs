using System.Security.Cryptography;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Shared;
using ProtonDrive.Shared.Extensions;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal class RemoteRevisionCreationProcess : IRevisionCreationProcess<string>
{
    private readonly HashingStream _contentStream;
    private readonly IReadOnlyCollection<UploadedBlock> _uploadedBlocks;
    private readonly int _blockSize;
    private readonly IRevisionSealer _revisionSealer;

    public RemoteRevisionCreationProcess(
        NodeInfo<string> fileInfo,
        Stream contentStream,
        IReadOnlyCollection<UploadedBlock> uploadedBlocks,
        int blockSize,
        IRevisionSealer revisionSealer)
    {
        Ensure.NotNull(fileInfo.Id, nameof(fileInfo), nameof(fileInfo.Id));

        FileInfo = fileInfo;

        _contentStream = new HashingStream(contentStream, HashAlgorithmName.SHA1);
        _uploadedBlocks = uploadedBlocks;
        _blockSize = blockSize;

        _revisionSealer = revisionSealer;
    }

    public NodeInfo<string> FileInfo { get; }
    public NodeInfo<string> BackupInfo { get; set; } = NodeInfo<string>.Empty();
    public bool ImmediateHydrationRequired => true;

    public Stream OpenContentStream()
    {
        return new SafeRemoteFileStream(_contentStream, FileInfo.Id);
    }

    public async Task<NodeInfo<string>> FinishAsync(CancellationToken cancellationToken)
    {
        try
        {
            ValidateUpload();

            var revisionSealingParameters = GetRevisionSealingParameters();

            await _revisionSealer.SealRevisionAsync(revisionSealingParameters, cancellationToken)
                .ConfigureAwait(false);

            return FileInfo.Copy().WithSizeOnStorage(_uploadedBlocks.Sum(b => (long)b.Size)).WithSha1Digest(revisionSealingParameters.Sha1Digest);
        }
        catch (Exception ex) when (ExceptionMapping.TryMapException(ex, FileInfo.Id, includeObjectId: false, out var mappedException))
        {
            throw mappedException;
        }
    }

    public void Dispose()
    {
        _contentStream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _contentStream.DisposeAsync();
    }

    protected virtual RevisionSealingParameters GetRevisionSealingParameters()
    {
        return new RevisionSealingParameters
        {
            Blocks = _uploadedBlocks,
            Sha1Digest = GetSha1Digest(),
        };
    }

    private void ValidateUpload()
    {
        var expectedNumberOfContentBlocks = (FileInfo.Size + _blockSize - 1) / _blockSize;

        var numberOfUploadedContentBlocks = 0;
        var numberOfPlainDataBytesRead = 0L;
        foreach (var block in _uploadedBlocks.Where(x => !x.IsThumbnail))
        {
            ++numberOfUploadedContentBlocks;
            numberOfPlainDataBytesRead += block.NumberOfPlainDataBytesRead;
        }

        if (numberOfPlainDataBytesRead != FileInfo.Size)
        {
            throw new FileSystemClientException("The number of bytes read from the file does not equal the expected size", FileSystemErrorCode.IntegrityFailure);
        }

        if (numberOfUploadedContentBlocks != expectedNumberOfContentBlocks)
        {
            throw new FileSystemClientException("The number of uploaded blocks does not equal the expected number", FileSystemErrorCode.IntegrityFailure);
        }
    }

    private string GetSha1Digest()
    {
        Span<byte> digestSpan = stackalloc byte[SHA1.HashSizeInBytes];
        _contentStream.GetCurrentHash(digestSpan);
        return Convert.ToHexStringLower(digestSpan);
    }
}
