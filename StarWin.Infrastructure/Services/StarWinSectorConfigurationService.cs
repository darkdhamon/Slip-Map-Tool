using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinSectorConfigurationService(StarWinDbContext dbContext) : IStarWinSectorConfigurationService
{
    public async Task<string> SaveSectorNameAsync(
        int sectorId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Sector name is required.");
        }

        var nameAlreadyUsed = await dbContext.Sectors
            .AnyAsync(
                sector => sector.Id != sectorId && sector.Name == normalizedName,
                cancellationToken);
        if (nameAlreadyUsed)
        {
            throw new InvalidOperationException("Another sector already uses that name.");
        }

        var sector = await dbContext.Sectors
            .FirstOrDefaultAsync(sector => sector.Id == sectorId, cancellationToken)
            ?? throw new InvalidOperationException("Sector was not found.");

        sector.Name = normalizedName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return normalizedName;
    }

    public async Task<SectorConfiguration> SaveHyperlaneSettingsAsync(
        int sectorId,
        decimal basicMaximumLengthParsecs,
        decimal ownedBaseMaximumLengthParsecs,
        CancellationToken cancellationToken = default)
    {
        var normalizedBasicLength = Math.Clamp(basicMaximumLengthParsecs, 0.1m, 100m);
        var defaultOwnedLength = normalizedBasicLength * 1.2m;
        var normalizedOwnedLength = Math.Clamp(
            ownedBaseMaximumLengthParsecs <= 0 ? defaultOwnedLength : ownedBaseMaximumLengthParsecs,
            normalizedBasicLength,
            150m);

        var configuration = await dbContext.Set<SectorConfiguration>()
            .FirstOrDefaultAsync(item => item.SectorId == sectorId, cancellationToken);

        if (configuration is null)
        {
            configuration = new SectorConfiguration { SectorId = sectorId };
            dbContext.Set<SectorConfiguration>().Add(configuration);
        }

        configuration.BasicHyperlaneMaximumLengthParsecs = normalizedBasicLength;
        configuration.OwnedHyperlaneBaseMaximumLengthParsecs = normalizedOwnedLength;
        configuration.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return configuration;
    }
}
