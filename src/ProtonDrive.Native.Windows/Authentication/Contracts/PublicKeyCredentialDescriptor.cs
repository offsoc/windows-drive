namespace ProtonDrive.Native.Authentication.Contracts;

internal sealed class PublicKeyCredentialDescriptor
{
    public required IReadOnlyList<byte> Id { get; init; }
    public required string Type { get; init; }
}
