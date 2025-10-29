namespace ProtonDrive.App.FileSystem.Metadata.GoogleTakeout;

internal static class GoogleTakeoutMetadataFileNameGenerator
{
    public static IEnumerable<string> GetFileNameCandidates(string fileName)
    {
        const string metadataFileExtension = ".supplemental-metadata";
        const string jsonExtension = ".json";
        const int maxFileNameLength = 255;

        // Google Takeout cuts photo file names to much less than 255 characters.
        // If the name is longer, it does not belong to Google Takeout.
        if (fileName.Length + metadataFileExtension.Length + jsonExtension.Length > maxFileNameLength)
        {
            yield break;
        }

        yield return fileName + metadataFileExtension + jsonExtension;

        yield return fileName + jsonExtension;

        for (var i = 1; i < metadataFileExtension.Length; i++)
        {
            yield return fileName + metadataFileExtension[..^i] + jsonExtension;
        }
    }
}
