using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinSectorConfigurationService
{
    Task<string> SaveSectorNameAsync(
        int sectorId,
        string name,
        CancellationToken cancellationToken = default);

    Task<SectorConfiguration> SaveHyperlaneSettingsAsync(
        int sectorId,
        SectorConfiguration configuration,
        CancellationToken cancellationToken = default);
}
