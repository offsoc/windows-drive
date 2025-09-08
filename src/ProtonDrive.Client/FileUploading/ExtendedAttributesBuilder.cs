using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proton.Security.Cryptography.Abstractions;
using ProtonDrive.Client.Contracts;
using ProtonDrive.Client.Cryptography;
using ProtonDrive.Sync.Shared.FileSystem;

namespace ProtonDrive.Client.FileUploading;

internal sealed class ExtendedAttributesBuilder : IExtendedAttributesBuilder
{
    private readonly ICryptographyService _cryptographyService;
    private readonly IFileMetadataProvider _fileMetadataProvider;
    private readonly ILogger<ExtendedAttributesBuilder> _logger;

    public ExtendedAttributesBuilder(
        ICryptographyService cryptographyService,
        IFileMetadataProvider fileMetadataProvider,
        ILogger<ExtendedAttributesBuilder> logger)
    {
        _cryptographyService = cryptographyService;
        _fileMetadataProvider = fileMetadataProvider;
        _logger = logger;
    }

    public PublicPgpKey? NodeKey { get; init; }
    public Address? SignatureAddress { get; init; }

    public long? Size { get; set; }
    public DateTime? LastWriteTime { get; set; }
    public IEnumerable<int>? BlockSizes { get; set; }
    public string? Sha1Digest { get; set; }

    public async Task<string?> BuildAsync(CancellationToken cancellationToken)
    {
        if (NodeKey is null)
        {
            throw new InvalidOperationException($"{nameof(NodeKey)} is required to encrypt extended attributes");
        }

        if (SignatureAddress is null)
        {
            throw new InvalidOperationException($"{nameof(SignatureAddress)} is required to encrypt extended attributes");
        }

        if (Size is null)
        {
            throw new InvalidOperationException($"{nameof(Size)} is required to encrypt extended attributes");
        }

        if (LastWriteTime is null)
        {
            throw new InvalidOperationException($"{nameof(LastWriteTime)} is required to encrypt extended attributes");
        }

        if (BlockSizes is null)
        {
            throw new InvalidOperationException($"{nameof(BlockSizes)} is required to encrypt extended attributes");
        }

        if (Sha1Digest is null)
        {
            throw new InvalidOperationException($"{nameof(Sha1Digest)} is required to encrypt extended attributes");
        }

        try
        {
            var encrypter = _cryptographyService.CreateExtendedAttributesEncrypter(NodeKey, SignatureAddress);

            var commonExtendedAttributes = GetCommonExtendedAttributes();

            var fileMetadata = await _fileMetadataProvider.GetMetadataAsync().ConfigureAwait(false);

            var locationExtendedAttributes = fileMetadata?.GetLocationExtendedAttributes();
            var cameraExtendedAttributes = fileMetadata?.GetCameraExtendedAttributes();
            var mediaExtendedAttributes = fileMetadata?.GetMediaExtendedAttributes();

            var extendedAttributes = new ExtendedAttributes(
                commonExtendedAttributes,
                locationExtendedAttributes,
                cameraExtendedAttributes,
                mediaExtendedAttributes);

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(extendedAttributes);

            var jsonStream = new MemoryStream(jsonBytes);

            var plainDataSource = new PlainDataSource(jsonStream);

            await using (plainDataSource.ConfigureAwait(false))
            {
                var messageStream = encrypter.GetEncryptingAndSigningStream(plainDataSource, PgpArmoring.Ascii, PgpCompression.Deflate);

                using var messageStreamReader = new StreamReader(messageStream, Encoding.ASCII);

                var result = await messageStreamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                return result;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The creation of extended attributes failed: {Message}", exception.Message);
            return null;
        }
    }

    private CommonExtendedAttributes GetCommonExtendedAttributes()
    {
        return new CommonExtendedAttributes
        {
            Size = Size,
            LastWriteTime = LastWriteTime,
            BlockSizes = BlockSizes,
            Digests = new Digests { Sha1 = Sha1Digest },
        };
    }
}
