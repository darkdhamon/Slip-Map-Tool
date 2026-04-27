using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerQueryService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinExplorerQueryService
{
    public async Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var systems = await dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == sectorId)
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .Select(system => new ExplorerLookupOption(system.Id, system.Name))
            .ToListAsync(cancellationToken);

        var systemCount = systems.Count;

        var worldCount = await (
            from world in dbContext.Worlds.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
            select world.Id)
            .CountAsync(cancellationToken);

        var colonyCount = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
            select colony.Id)
            .CountAsync(cancellationToken);

        var empireIds = new HashSet<int>();
        empireIds.UnionWith(await dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == sectorId && system.AllegianceId != ushort.MaxValue)
            .Select(system => (int)system.AllegianceId)
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

        var raceIds = new HashSet<int>();
        raceIds.UnionWith(await (
            from world in dbContext.Worlds.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && world.AlienRaceId.HasValue
            select (int)world.AlienRaceId!.Value)
            .ToListAsync(cancellationToken));
        raceIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
            select (int)colony.RaceId)
            .ToListAsync(cancellationToken));

        var empires = empireIds.Count == 0
            ? []
            : await dbContext.Empires
                .AsNoTracking()
                .Where(empire => empireIds.Contains(empire.Id))
                .OrderBy(empire => empire.Name)
                .ThenBy(empire => empire.Id)
                .Select(empire => new ExplorerLookupOption(empire.Id, empire.Name))
                .ToListAsync(cancellationToken);

        return new ExplorerSectorOverviewData(
            sectorId,
            systemCount,
            worldCount,
            colonyCount,
            empires.Count,
            raceIds.Count,
            systems,
            empires);
    }

    public async Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var raceIds = await LoadSectorRaceIdsAsync(dbContext, request.SectorId, cancellationToken);
        if (raceIds.Count == 0)
        {
            return new ExplorerAlienRaceListPage([], false);
        }

        var races = await dbContext.AlienRaces
            .AsNoTracking()
            .Where(race => raceIds.Contains(race.Id))
            .OrderBy(race => race.Name)
            .ThenBy(race => race.Id)
            .Select(race => new ExplorerAlienRaceListItem(
                race.Id,
                race.Name,
                race.AppearanceType,
                race.EnvironmentType))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = races.Count > request.Limit;
        return new ExplorerAlienRaceListPage(races.Take(request.Limit).ToList(), hasMore);
    }

    public async Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var raceIds = await LoadSectorRaceIdsAsync(dbContext, sectorId, cancellationToken);
        if (!raceIds.Contains(raceId))
        {
            return null;
        }

        return await dbContext.AlienRaces
            .AsNoTracking()
            .Where(race => race.Id == raceId)
            .Select(race => new ExplorerAlienRaceListItem(
                race.Id,
                race.Name,
                race.AppearanceType,
                race.EnvironmentType))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExplorerAlienRaceDetail?> LoadAlienRaceDetailAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var raceIds = await LoadSectorRaceIdsAsync(dbContext, sectorId, cancellationToken);
        if (!raceIds.Contains(raceId))
        {
            return null;
        }

        var race = await dbContext.AlienRaces
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == raceId, cancellationToken);
        if (race is null)
        {
            return null;
        }

        var homeWorld = race.HomePlanetId > 0
            ? await dbContext.Worlds
                .AsNoTracking()
                .Where(world => world.Id == race.HomePlanetId)
                .Select(world => new StarWin.Domain.Model.Entity.StarMap.World
                {
                    Id = world.Id,
                    Name = world.Name,
                    WorldType = world.WorldType,
                    StarSystemId = world.StarSystemId
                })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
        var empires = sectorEmpireIds.Count == 0
            ? []
            : await dbContext.Empires
                .AsNoTracking()
                .AsSplitQuery()
                .Where(empire => sectorEmpireIds.Contains(empire.Id) && empire.RaceMemberships.Any(membership => membership.RaceId == raceId))
                .Include(empire => empire.RaceMemberships)
                .Include(empire => empire.Religions)
                .OrderBy(empire => empire.Name)
                .ThenBy(empire => empire.Id)
                .ToListAsync(cancellationToken);

        return new ExplorerAlienRaceDetail(sectorId, race, homeWorld, empires);
    }

    public async Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId && !string.IsNullOrEmpty(history.EventType))
            .Select(history => history.EventType)
            .Distinct()
            .OrderBy(eventType => eventType)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == request.SectorId);

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query = query.Where(history => history.EventType == request.EventType);
        }

        var events = await query
            .OrderBy(history => history.Century)
            .ThenBy(history => history.EventType)
            .ThenBy(history => history.Description)
            .Select(history => new ExplorerTimelineListItem(
                EF.Property<int>(history, "Id"),
                BuildTimelineTitle(history.Description, history.EventType),
                history.EventType,
                BuildTimelineTimeLabel(history.Century),
                history.Century,
                history.RaceId,
                history.OtherRaceId,
                history.EmpireId,
                history.ColonyId,
                history.PlanetId,
                history.StarSystemId))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = events.Count > request.Limit;
        return new ExplorerTimelinePage(events.Take(request.Limit).ToList(), hasMore);
    }

    public async Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var historyEntity = await dbContext.HistoryEvents
            .FirstOrDefaultAsync(item => EF.Property<int>(item, "Id") == eventId, cancellationToken);

        if (historyEntity is null)
        {
            return null;
        }

        if (historyEntity.ColonyId is null && historyEntity.PlanetId is int colonyWorldId)
        {
            var derivedColonyId = await dbContext.Colonies
                .Where(item => item.WorldId == colonyWorldId)
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (derivedColonyId is int resolvedColonyId)
            {
                historyEntity.ColonyId = resolvedColonyId;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        var history = new
        {
            EventId = eventId,
            historyEntity.Century,
            historyEntity.EventType,
            historyEntity.Description,
            historyEntity.ImportDataJson,
            historyEntity.RaceId,
            historyEntity.OtherRaceId,
            historyEntity.EmpireId,
            historyEntity.ColonyId,
            historyEntity.PlanetId,
            historyEntity.StarSystemId
        };

        ExplorerLookupOption? race = null;
        ExplorerLookupOption? otherRace = null;
        ExplorerLookupOption? empire = null;
        ExplorerLookupOption? colony = null;
        ExplorerLookupOption? world = null;
        ExplorerLookupOption? system = null;

        if (history.RaceId is int raceId)
        {
            race = await dbContext.AlienRaces.AsNoTracking()
                .Where(item => item.Id == raceId)
                .Select(item => new ExplorerLookupOption(item.Id, item.Name))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (history.OtherRaceId is int otherRaceId)
        {
            otherRace = await dbContext.AlienRaces.AsNoTracking()
                .Where(item => item.Id == otherRaceId)
                .Select(item => new ExplorerLookupOption(item.Id, item.Name))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (history.EmpireId is int empireId)
        {
            empire = await dbContext.Empires.AsNoTracking()
                .Where(item => item.Id == empireId)
                .Select(item => new ExplorerLookupOption(item.Id, item.Name))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (history.ColonyId is int colonyId)
        {
            colony = await (
                from colonyItem in dbContext.Colonies.AsNoTracking()
                join worldItem in dbContext.Worlds.AsNoTracking() on colonyItem.WorldId equals worldItem.Id
                where colonyItem.Id == colonyId
                select new ExplorerLookupOption(colonyItem.Id, $"{colonyItem.ColonyClass} on {worldItem.Name}"))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (history.PlanetId is int worldId)
        {
            world = await dbContext.Worlds.AsNoTracking()
                .Where(item => item.Id == worldId)
                .Select(item => new ExplorerLookupOption(item.Id, item.Name))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (history.StarSystemId is int systemId)
        {
            system = await dbContext.StarSystems.AsNoTracking()
                .Where(item => item.Id == systemId)
                .Select(item => new ExplorerLookupOption(item.Id, item.Name))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ExplorerTimelineEventDetail(
            history.EventId,
            BuildTimelineTitle(history.Description, history.EventType),
            history.EventType,
            BuildTimelineTimeLabel(history.Century),
            history.Century,
            history.Description,
            FormatImportDataJson(history.ImportDataJson),
            race,
            otherRace,
            empire,
            colony,
            world,
            system);
    }

    private static string? FormatImportDataJson(string? importDataJson)
    {
        if (string.IsNullOrWhiteSpace(importDataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(importDataJson);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (JsonException)
        {
            return importDataJson;
        }
    }

    private static string BuildTimelineTitle(string description, string eventType)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return eventType;
        }

        const int maxLength = 120;
        return description.Length <= maxLength
            ? description
            : $"{description[..(maxLength - 1)].TrimEnd()}...";
    }

    private static string BuildTimelineTimeLabel(int century)
    {
        return $"Century {century}";
    }

    private static async Task<HashSet<int>> LoadSectorRaceIdsAsync(StarWinDbContext dbContext, int sectorId, CancellationToken cancellationToken)
    {
        var raceIds = new HashSet<int>();

        raceIds.UnionWith(await (
            from world in dbContext.Worlds.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && world.AlienRaceId.HasValue
            select (int)world.AlienRaceId!.Value)
            .ToListAsync(cancellationToken));

        raceIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
            select (int)colony.RaceId)
            .ToListAsync(cancellationToken));

        raceIds.UnionWith(await (
            from demographic in dbContext.Set<StarWin.Domain.Model.Entity.Civilization.ColonyDemographic>().AsNoTracking()
            join colony in dbContext.Colonies.AsNoTracking() on demographic.ColonyId equals colony.Id
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
            select demographic.RaceId)
            .ToListAsync(cancellationToken));

        raceIds.UnionWith(await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId && history.RaceId.HasValue)
            .Select(history => history.RaceId!.Value)
            .ToListAsync(cancellationToken));

        raceIds.UnionWith(await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId && history.OtherRaceId.HasValue)
            .Select(history => history.OtherRaceId!.Value)
            .ToListAsync(cancellationToken));

        return raceIds;
    }

    private static async Task<HashSet<int>> LoadSectorEmpireIdsAsync(StarWinDbContext dbContext, int sectorId, CancellationToken cancellationToken)
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
}
