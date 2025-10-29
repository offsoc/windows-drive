namespace ProtonDrive.Client.FileUploading;

internal interface IRevisionSealer
{
    Task SealRevisionAsync(RevisionSealingParameters parameters, CancellationToken cancellationToken);
}
