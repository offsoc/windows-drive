using System;

namespace ProtonDrive.Client.FileUploading;

internal sealed record PhotoRevisionSealingParameters : RevisionSealingParameters
{
    public required DateTime DefaultCaptureTimeUtc { get; init; }
    public string? MainPhotoLinkId { get; init; }
}
