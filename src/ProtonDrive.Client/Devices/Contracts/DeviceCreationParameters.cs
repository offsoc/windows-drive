using ProtonDrive.Client.Shares.Contracts;

namespace ProtonDrive.Client.Devices.Contracts;

internal sealed class DeviceCreationParameters
{
    public required DeviceDeviceCreationParameters Device { get; init; }
    public required ShareCreationParameters Share { get; init; }
    public required LinkCreationParameters Link { get; init; }
}
