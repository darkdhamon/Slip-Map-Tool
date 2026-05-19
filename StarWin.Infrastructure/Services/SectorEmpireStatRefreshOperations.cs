using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

internal static class SectorEmpireStatRefreshOperations
{
    public static async Task<SectorEmpireStatsRefreshResult> RebuildSectorAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (sectorId <= 0)
        {
            throw new InvalidOperationException("Sector was not found.");
        }

        var sectorExists = await dbContext.Sectors
            .AsNoTracking()
            .AnyAsync(sector => sector.Id == sectorId, cancellationToken);
        if (!sectorExists)
        {
            throw new InvalidOperationException("Sector was not found.");
        }

        var calculatedAtUtc = DateTime.UtcNow;
        var empireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
        var controlledCountsByEmpireId = await LoadControlledWorldCountsByEmpireIdAsync(dbContext, sectorId, cancellationToken);
        var trackedCountsByEmpireId = await LoadTrackedWorldCountsByEmpireIdAsync(dbContext, sectorId, cancellationToken);

        var existingRowsByEmpireId = await dbContext.SectorEmpireStats
            .Where(stat => stat.SectorId == sectorId)
            .ToDictionaryAsync(stat => stat.EmpireId, cancellationToken);

        foreach (var obsoleteRow in existingRowsByEmpireId.Values.Where(row => !empireIds.Contains(row.EmpireId)).ToList())
        {
            dbContext.SectorEmpireStats.Remove(obsoleteRow);
        }

        foreach (var empireId in empireIds.OrderBy(id => id))
        {
            var controlledWorldCount = controlledCountsByEmpireId.GetValueOrDefault(empireId);
            var trackedWorldCount = trackedCountsByEmpireId.GetValueOrDefault(empireId);
            if (!existingRowsByEmpireId.TryGetValue(empireId, out var row))
            {
                row = new SectorEmpireStat
                {
                    SectorId = sectorId,
                    EmpireId = empireId
                };
                dbContext.SectorEmpireStats.Add(row);
            }

            row.ControlledWorldCount = controlledWorldCount;
            row.TrackedWorldCount = trackedWorldCount;
            row.LastCalculatedAtUtc = calculatedAtUtc;
        }

        var configuration = await EnsureSectorConfigurationAsync(dbContext, sectorId, cancellationToken);
        configuration.SectorEmpireStatsCalculatedAtUtc = calculatedAtUtc;
        configuration.SectorEmpireStatsInvalidatedAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SectorEmpireStatsRefreshResult(sectorId, empireIds.Count, calculatedAtUtc);
    }

    public static async Task<SectorEmpireStatRefreshResult?> RefreshEmpireAsync(
        StarWinDbContext dbContext,
        int sectorId,
        int empireId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (sectorId <= 0 || empireId <= 0)
        {
            return null;
        }

        var calculatedAtUtc = DateTime.UtcNow;
        var isPresentInSector = await EmpireIsPresentInSectorAsync(dbContext, sectorId, empireId, cancellationToken);
        var existingRow = await dbContext.SectorEmpireStats
            .FirstOrDefaultAsync(
                stat => stat.SectorId == sectorId && stat.EmpireId == empireId,
                cancellationToken);

        if (!isPresentInSector)
        {
            if (existingRow is not null)
            {
                dbContext.SectorEmpireStats.Remove(existingRow);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        var controlledWorldCount = await LoadControlledWorldCountAsync(dbContext, sectorId, empireId, cancellationToken);
        var trackedWorldCount = await LoadTrackedWorldCountAsync(dbContext, sectorId, empireId, cancellationToken);
        if (existingRow is null)
        {
            existingRow = new SectorEmpireStat
            {
                SectorId = sectorId,
                EmpireId = empireId
            };
            dbContext.SectorEmpireStats.Add(existingRow);
        }

        existingRow.ControlledWorldCount = controlledWorldCount;
        existingRow.TrackedWorldCount = trackedWorldCount;
        existingRow.LastCalculatedAtUtc = calculatedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SectorEmpireStatRefreshResult(
            sectorId,
            empireId,
            controlledWorldCount,
            trackedWorldCount,
            calculatedAtUtc);
    }

    public static async Task MarkSectorStatsStaleAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (sectorId <= 0)
        {
            return;
        }

        var configuration = await EnsureSectorConfigurationAsync(dbContext, sectorId, cancellationToken);
        configuration.SectorEmpireStatsInvalidatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<SectorConfiguration> EnsureSectorConfigurationAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.Set<SectorConfiguration>()
            .FirstOrDefaultAsync(item => item.SectorId == sectorId, cancellationToken);
        if (configuration is not null)
        {
            return configuration;
        }

        configuration = new SectorConfiguration { SectorId = sectorId };
        dbContext.Set<SectorConfiguration>().Add(configuration);
        return configuration;
    }

    private static async Task<HashSet<int>> LoadSectorEmpireIdsAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken)
    {
        var empireIds = new HashSet<int>();

        empireIds.UnionWith(await dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == sectorId && system.AllegianceId != ushort.MaxValue)
            .Select(system => (int)system.AllegianceId)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from habitat in dbContext.SpaceHabitats.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking()
                on EF.Property<int>(habitat, "StarSystemId") equals system.Id
            where system.SectorId == sectorId && habitat.BuiltByEmpireId.HasValue
            select habitat.BuiltByEmpireId!.Value)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from habitat in dbContext.SpaceHabitats.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking()
                on EF.Property<int>(habitat, "StarSystemId") equals system.Id
            where system.SectorId == sectorId && habitat.ControlledByEmpireId.HasValue
            select habitat.ControlledByEmpireId!.Value)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from world in dbContext.Worlds.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && world.ControlledByEmpireId.HasValue
            select world.ControlledByEmpireId!.Value)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from world in dbContext.Worlds.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && world.AllegianceId != ushort.MaxValue
            select (int)world.AllegianceId)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && colony.ControllingEmpireId.HasValue
            select colony.ControllingEmpireId!.Value)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && colony.FoundingEmpireId.HasValue
            select colony.FoundingEmpireId!.Value)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && colony.ParentEmpireId.HasValue
            select colony.ParentEmpireId!.Value)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && colony.AllegianceId != ushort.MaxValue
            select (int)colony.AllegianceId)
            .ToListAsync(cancellationToken));

        empireIds.UnionWith(await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId && history.EmpireId.HasValue)
            .Select(history => history.EmpireId!.Value)
            .ToListAsync(cancellationToken));

        return empireIds;
    }

    private static async Task<Dictionary<int, int>> LoadControlledWorldCountsByEmpireIdAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken)
    {
        var groupedCounts = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && colony.ControllingEmpireId.HasValue
            group colony.WorldId by colony.ControllingEmpireId!.Value into grouped
            select new
            {
                EmpireId = grouped.Key,
                ControlledWorldCount = grouped.Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        return groupedCounts.ToDictionary(item => item.EmpireId, item => item.ControlledWorldCount);
    }

    private static async Task<Dictionary<int, int>> LoadTrackedWorldCountsByEmpireIdAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken)
    {
        var trackedLinks = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && (colony.ControllingEmpireId.HasValue || colony.FoundingEmpireId.HasValue)
            select new
            {
                colony.WorldId,
                colony.ControllingEmpireId,
                colony.FoundingEmpireId
            })
            .ToListAsync(cancellationToken);

        return trackedLinks
            .SelectMany(link =>
            {
                var pairs = new List<(int EmpireId, int WorldId)>(2);
                if (link.ControllingEmpireId is int controllingEmpireId)
                {
                    pairs.Add((controllingEmpireId, link.WorldId));
                }

                if (link.FoundingEmpireId is int foundingEmpireId)
                {
                    pairs.Add((foundingEmpireId, link.WorldId));
                }

                return pairs;
            })
            .Distinct()
            .GroupBy(item => item.EmpireId)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static async Task<int> LoadControlledWorldCountAsync(
        StarWinDbContext dbContext,
        int sectorId,
        int empireId,
        CancellationToken cancellationToken)
    {
        return await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && colony.ControllingEmpireId == empireId
            select colony.WorldId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private static async Task<int> LoadTrackedWorldCountAsync(
        StarWinDbContext dbContext,
        int sectorId,
        int empireId,
        CancellationToken cancellationToken)
    {
        return await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && (colony.ControllingEmpireId == empireId || colony.FoundingEmpireId == empireId)
            select colony.WorldId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private static async Task<bool> EmpireIsPresentInSectorAsync(
        StarWinDbContext dbContext,
        int sectorId,
        int empireId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StarSystems
                   .AsNoTracking()
                   .AnyAsync(system => system.SectorId == sectorId && system.AllegianceId == empireId, cancellationToken)
               || await (
                   from habitat in dbContext.SpaceHabitats.AsNoTracking()
                   join system in dbContext.StarSystems.AsNoTracking()
                       on EF.Property<int>(habitat, "StarSystemId") equals system.Id
                   where system.SectorId == sectorId
                       && (habitat.BuiltByEmpireId == empireId || habitat.ControlledByEmpireId == empireId)
                   select habitat.Id)
                   .AnyAsync(cancellationToken)
               || await (
                   from world in dbContext.Worlds.AsNoTracking()
                   join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
                   where system.SectorId == sectorId
                       && (world.ControlledByEmpireId == empireId || world.AllegianceId == empireId)
                   select world.Id)
                   .AnyAsync(cancellationToken)
               || await (
                   from colony in dbContext.Colonies.AsNoTracking()
                   join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
                   join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
                   where system.SectorId == sectorId
                       && (colony.ControllingEmpireId == empireId
                           || colony.FoundingEmpireId == empireId
                           || colony.ParentEmpireId == empireId
                           || colony.AllegianceId == empireId)
                   select colony.Id)
                   .AnyAsync(cancellationToken)
               || await dbContext.HistoryEvents
                   .AsNoTracking()
                   .AnyAsync(history => history.SectorId == sectorId && history.EmpireId == empireId, cancellationToken);
    }
}
