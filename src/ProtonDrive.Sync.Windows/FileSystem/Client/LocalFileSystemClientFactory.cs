using Microsoft.Extensions.Logging;
using ProtonDrive.Sync.Shared.FileSystem;
using ProtonDrive.Sync.Windows.FileSystem.Photos;

namespace ProtonDrive.Sync.Windows.FileSystem.Client;

public sealed class LocalFileSystemClientFactory : ILocalFileSystemClientFactory
{
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IFileMetadataGenerator _fileMetadataGenerator;
    private readonly IPhotoTagsGenerator _photoTagsGenerator;
    private readonly ILoggerFactory _loggerFactory;

    public LocalFileSystemClientFactory(
        IThumbnailGenerator thumbnailGenerator,
        IFileMetadataGenerator fileMetadataGenerator,
        IPhotoTagsGenerator photoTagsGenerator,
        ILoggerFactory loggerFactory)
    {
        _thumbnailGenerator = thumbnailGenerator;
        _fileMetadataGenerator = fileMetadataGenerator;
        _photoTagsGenerator = photoTagsGenerator;
        _loggerFactory = loggerFactory;
    }

    public IFileSystemClient<long> CreateClassicClient()
    {
        return new ClassicFileSystemClient(_thumbnailGenerator, _fileMetadataGenerator, _photoTagsGenerator);
    }

    public IFileSystemClient<long> CreateOnDemandHydrationClient()
    {
        return new OnDemandHydrationFileSystemClient(_thumbnailGenerator, _fileMetadataGenerator, _photoTagsGenerator, _loggerFactory);
    }

    public IPhotoFileSystemClient<long> CreatePhotoClient()
    {
        return new PhotoFileSystemClient(CreateClassicClient());
    }
}
