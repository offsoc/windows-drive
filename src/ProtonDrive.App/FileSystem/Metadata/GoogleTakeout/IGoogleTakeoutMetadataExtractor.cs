namespace ProtonDrive.App.FileSystem.Metadata.GoogleTakeout;

public interface IGoogleTakeoutMetadataExtractor
{
    GoogleTakeoutMetadata? ExtractMetadata(string filePath);
}
