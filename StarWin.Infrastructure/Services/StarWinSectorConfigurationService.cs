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
        SectorConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Set<SectorConfiguration>()
            .FirstOrDefaultAsync(item => item.SectorId == sectorId, cancellationToken);

        if (entity is null)
        {
            entity = new SectorConfiguration { SectorId = sectorId };
            dbContext.Set<SectorConfiguration>().Add(entity);
        }

        entity.OffLaneMaximumDistanceParsecs = NormalizeDistance(configuration.OffLaneMaximumDistanceParsecs, 0.1m, 20m, 2m);

        entity.Tl6HyperlaneName = NormalizeTierName(configuration.Tl6HyperlaneName, "Basic Hyperlane");
        entity.Tl6MaximumDistanceParsecs = NormalizeDistance(configuration.Tl6MaximumDistanceParsecs, 0.1m, 20m, 1m);
        entity.Tl6OffLaneSpeedMultiplier = NormalizeMultiplier(configuration.Tl6OffLaneSpeedMultiplier, 0.1m, 100m, 2m);
        entity.Tl6HyperlaneSpeedModifier = NormalizeMultiplier(configuration.Tl6HyperlaneSpeedModifier, 0.1m, 10m, 2m);

        entity.Tl7HyperlaneName = NormalizeTierName(configuration.Tl7HyperlaneName, "Enhanced Hyperlane");
        entity.Tl7MaximumDistanceParsecs = NormalizeDistance(configuration.Tl7MaximumDistanceParsecs, 0.1m, 20m, 1.2m);
        entity.Tl7OffLaneSpeedMultiplier = NormalizeMultiplier(configuration.Tl7OffLaneSpeedMultiplier, 0.1m, 100m, 4m);
        entity.Tl7HyperlaneSpeedModifier = NormalizeMultiplier(configuration.Tl7HyperlaneSpeedModifier, 0.1m, 10m, 2.25m);

        entity.Tl8HyperlaneName = NormalizeTierName(configuration.Tl8HyperlaneName, "Advanced Hyperlane");
        entity.Tl8MaximumDistanceParsecs = NormalizeDistance(configuration.Tl8MaximumDistanceParsecs, 0.1m, 20m, 1.4m);
        entity.Tl8OffLaneSpeedMultiplier = NormalizeMultiplier(configuration.Tl8OffLaneSpeedMultiplier, 0.1m, 100m, 8m);
        entity.Tl8HyperlaneSpeedModifier = NormalizeMultiplier(configuration.Tl8HyperlaneSpeedModifier, 0.1m, 10m, 2.5m);

        entity.Tl9HyperlaneName = NormalizeTierName(configuration.Tl9HyperlaneName, "Prime Hyperlane");
        entity.Tl9MaximumDistanceParsecs = NormalizeDistance(configuration.Tl9MaximumDistanceParsecs, 0.1m, 20m, 1.6m);
        entity.Tl9OffLaneSpeedMultiplier = NormalizeMultiplier(configuration.Tl9OffLaneSpeedMultiplier, 0.1m, 100m, 16m);
        entity.Tl9HyperlaneSpeedModifier = NormalizeMultiplier(configuration.Tl9HyperlaneSpeedModifier, 0.1m, 10m, 2.75m);

        entity.Tl10HyperlaneName = NormalizeTierName(configuration.Tl10HyperlaneName, "Ascendant Hyperlane");
        entity.Tl10MaximumDistanceParsecs = NormalizeUnlimitedDistance(configuration.Tl10MaximumDistanceParsecs, 20m, -1m);
        entity.Tl10OffLaneSpeedMultiplier = NormalizeMultiplier(configuration.Tl10OffLaneSpeedMultiplier, 0.1m, 100m, 32m);
        entity.Tl10HyperlaneSpeedModifier = NormalizeMultiplier(configuration.Tl10HyperlaneSpeedModifier, 0.1m, 10m, 3m);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static decimal NormalizeDistance(decimal value, decimal minimum, decimal maximum, decimal fallback)
    {
        return Math.Clamp(value <= 0 ? fallback : value, minimum, maximum);
    }

    private static decimal NormalizeUnlimitedDistance(decimal value, decimal maximum, decimal fallback)
    {
        if (value < 0)
        {
            return -1m;
        }

        return Math.Clamp(value == 0 ? fallback : value, 0.1m, maximum);
    }

    private static decimal NormalizeMultiplier(decimal value, decimal minimum, decimal maximum, decimal fallback)
    {
        return Math.Clamp(value <= 0 ? fallback : value, minimum, maximum);
    }

    private static string NormalizeTierName(string? value, string fallback)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
