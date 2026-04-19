using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinSectorConfigurationService(StarWinDbContext dbContext) : IStarWinSectorConfigurationService
{
    public async Task<SectorConfiguration> SaveBasicHyperlaneSettingsAsync(
        int sectorId,
        string tierName,
        decimal maximumLengthParsecs,
        CancellationToken cancellationToken = default)
    {
        var normalizedLength = Math.Clamp(maximumLengthParsecs, 0.1m, 100m);
        var normalizedName = string.IsNullOrWhiteSpace(tierName) ? "Basic" : tierName.Trim();

        var configuration = await dbContext.Set<SectorConfiguration>()
            .FirstOrDefaultAsync(item => item.SectorId == sectorId, cancellationToken);

        if (configuration is null)
        {
            configuration = new SectorConfiguration { SectorId = sectorId };
            dbContext.Set<SectorConfiguration>().Add(configuration);
        }

        configuration.BasicHyperlaneTierName = normalizedName;
        configuration.BasicHyperlaneMaximumLengthParsecs = normalizedLength;
        configuration.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return configuration;
    }
}
