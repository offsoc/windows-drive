using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProtonDrive.Shared.Extensions;

namespace ProtonDrive.App.FileSystem.Metadata.GoogleTakeout;

internal sealed class GoogleTakeoutMetadataExtractor : IGoogleTakeoutMetadataExtractor
{
    private readonly ILogger<GoogleTakeoutMetadataExtractor> _logger;

    public GoogleTakeoutMetadataExtractor(ILogger<GoogleTakeoutMetadataExtractor> logger)
    {
        _logger = logger;
    }

    public GoogleTakeoutMetadata? ExtractMetadata(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var folderName = Path.GetDirectoryName(filePath) ?? throw new ArgumentException("File path denotes a root directory", nameof(filePath));

        return (from metadataFileName in GoogleTakeoutMetadataFileNameGenerator.GetFileNameCandidates(fileName)
                select Path.Combine(folderName, metadataFileName) into metadataFilePath
                where File.Exists(metadataFilePath)
                select TryExtractMetadata(metadataFilePath)).FirstOrDefault();
    }

    private GoogleTakeoutMetadata? TryExtractMetadata(string metadataFilePath)
    {
        var metadata = DeserializeMetadata(metadataFilePath);

        if (metadata is null)
        {
            return null;
        }

        var captureTime = metadata.TakenTime.GetCaptureTime();
        var geoLocation = metadata.GeoData.GetGeoLocation() ?? metadata.GeoDataExif.GetGeoLocation();

        if (captureTime is null && geoLocation is null)
        {
            return null;
        }

        return new GoogleTakeoutMetadata
        {
            CaptureTime = captureTime,
            Latitude = geoLocation?.Latitude,
            Longitude = geoLocation?.Longitude,
        };
    }

    private GoogleTakeoutMetadataContract? DeserializeMetadata(string metadataFilePath)
    {
        try
        {
            using var reader = new FileStream(metadataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

            return JsonSerializer.Deserialize<GoogleTakeoutMetadataContract>(reader);
        }
        catch (Exception ex) when (ex.IsFileAccessException() || ex is JsonException)
        {
            _logger.LogWarning("Failed to deserialize Google Takeout metadata: {ErrorMessage}", ex.CombinedMessage());

            return null;
        }
    }
}
