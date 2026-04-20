using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinSectorConfigurationService
{
    Task<SectorConfiguration> SaveHyperlaneSettingsAsync(
        int sectorId,
        decimal basicMaximumLengthParsecs,
        decimal ownedBaseMaximumLengthParsecs,
        CancellationToken cancellationToken = default);
}
