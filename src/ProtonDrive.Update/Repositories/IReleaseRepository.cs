using ProtonDrive.Update.Releases;

namespace ProtonDrive.Update.Repositories;

internal interface IReleaseRepository
{
    Task<IEnumerable<Release>> GetReleasesAsync();

    IEnumerable<Release> GetReleasesFromCache();

    void ClearReleasesCache();
}
