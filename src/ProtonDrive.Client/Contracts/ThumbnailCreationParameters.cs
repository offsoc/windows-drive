using System;

namespace ProtonDrive.Client.Contracts;

internal sealed class ThumbnailCreationParameters : BlockCreationParametersBase
{
    public ThumbnailCreationParameters(int size, ThumbnailType type, ReadOnlyMemory<byte> hash)
    {
        Size = size;
        Type = type;
        Hash = hash;
    }

    public ThumbnailType Type { get; }
}
