using ProtonDrive.Client.Shares.Contracts;

namespace ProtonDrive.Client.Volumes.Contracts;

internal sealed class PhotoVolumeCreationParameters
{
    public required ShareCreationParameters Share { get; init; }
    public required LinkCreationParameters Link { get; init; }
}
