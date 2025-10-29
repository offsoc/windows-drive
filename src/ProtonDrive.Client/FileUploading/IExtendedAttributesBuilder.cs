using Proton.Security.Cryptography.Abstractions;

namespace ProtonDrive.Client.FileUploading;

internal interface IExtendedAttributesBuilder
{
    long? Size { get; set; }
    DateTime? LastWriteTime { get; set; }
    IEnumerable<int>? BlockSizes { get; set; }
    PublicPgpKey? NodeKey { get; init; }
    string? Sha1Digest { get; set; }
    DateTime? CaptureTime { get; }

    Task<string?> BuildAsync(CancellationToken cancellationToken);
}
