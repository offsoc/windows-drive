using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace ProtonDrive.Client.Contracts;

public sealed record BlockRequestResponse : ApiResponse
{
    private IImmutableList<UploadUrl>? _uploadUrls;
    private IImmutableList<ThumbnailUploadUrl>? _thumbnailUploadUrls;

    [JsonPropertyName("UploadLinks")]
    public IImmutableList<UploadUrl> UploadUrls
    {
        get => _uploadUrls ??= ImmutableList<UploadUrl>.Empty;
        init => _uploadUrls = value;
    }

    [JsonPropertyName("ThumbnailLinks")]
    public IImmutableList<ThumbnailUploadUrl> ThumbnailUrls
    {
        get => _thumbnailUploadUrls ??= ImmutableList<ThumbnailUploadUrl>.Empty;
        init => _thumbnailUploadUrls = value;
    }
}
