using System;
using System.Text.Json.Serialization;
using ProtonDrive.Shared.Text.Serialization;

namespace ProtonDrive.Client.Contracts;

internal abstract class BlockCreationParametersBase
{
    public int Size { get; init; }

    [JsonConverter(typeof(Base64JsonConverter))]
    public ReadOnlyMemory<byte> Hash { get; init; }
}
