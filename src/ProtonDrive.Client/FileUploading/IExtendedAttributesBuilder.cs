using Proton.Cryptography.Pgp;

namespace ProtonDrive.Client.FileUploading;

internal interface IExtendedAttributesBuilder
{
    long? Size { get; set; }
    DateTime? LastWriteTime { get; set; }
    IEnumerable<int>? BlockSizes { get; set; }
    PgpPublicKey? NodeKey { get; init; }
    string? Sha1Digest { get; set; }
    DateTime? CaptureTime { get; }

    Task<string?> BuildAsync(CancellationToken cancellationToken);
}
