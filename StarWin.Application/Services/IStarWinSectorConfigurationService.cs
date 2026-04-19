using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinSectorConfigurationService
{
    Task<SectorConfiguration> SaveBasicHyperlaneSettingsAsync(
        int sectorId,
        string tierName,
        decimal maximumLengthParsecs,
        CancellationToken cancellationToken = default);
}
