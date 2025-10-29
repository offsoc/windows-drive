using ProtonDrive.Client.Contracts;

namespace ProtonDrive.Client.FileUploading;

internal record RevisionSealingParameters
{
    public required IReadOnlyCollection<UploadedBlock> Blocks { get; init; }
    public required string Sha1Digest { get; init; }
}
