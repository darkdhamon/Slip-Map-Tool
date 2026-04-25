using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerQueryService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinExplorerQueryService
{
    private const int NoFilterId = -1;

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

    public async Task<ExplorerTimelineOptions> LoadTimelineOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var raceIds = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId)
            .SelectMany(history => new int?[] { history.RaceId, history.OtherRaceId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var empireIds = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId)
            .Where(history => history.EmpireId.HasValue)
            .Select(history => history.EmpireId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var colonyIds = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId)
            .Where(history => history.ColonyId.HasValue)
            .Select(history => history.ColonyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var worldIds = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId)
            .Where(history => history.PlanetId.HasValue)
            .Select(history => history.PlanetId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var systemIds = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId)
            .Where(history => history.StarSystemId.HasValue)
            .Select(history => history.StarSystemId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var races = raceIds.Count == 0
            ? []
            : await dbContext.AlienRaces
                .AsNoTracking()
                .Where(race => raceIds.Contains(race.Id))
                .OrderBy(race => race.Name)
                .ThenBy(race => race.Id)
                .Select(race => new ExplorerLookupOption(race.Id, race.Name))
                .ToListAsync(cancellationToken);

        var empires = empireIds.Count == 0
            ? []
            : await dbContext.Empires
                .AsNoTracking()
                .Where(empire => empireIds.Contains(empire.Id))
                .OrderBy(empire => empire.Name)
                .ThenBy(empire => empire.Id)
                .Select(empire => new ExplorerLookupOption(empire.Id, empire.Name))
                .ToListAsync(cancellationToken);

        var colonies = colonyIds.Count == 0
            ? []
            : await (
                from colony in dbContext.Colonies.AsNoTracking()
                join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
                where colonyIds.Contains(colony.Id)
                orderby world.Name, colony.Id
                select new ExplorerLookupOption(colony.Id, $"{colony.ColonyClass} on {world.Name}"))
                .ToListAsync(cancellationToken);

        var worlds = worldIds.Count == 0
            ? []
            : await dbContext.Worlds
                .AsNoTracking()
                .Where(world => worldIds.Contains(world.Id))
                .OrderBy(world => world.Name)
                .ThenBy(world => world.Id)
                .Select(world => new ExplorerLookupOption(world.Id, world.Name))
                .ToListAsync(cancellationToken);

        var systems = systemIds.Count == 0
            ? []
            : await dbContext.StarSystems
                .AsNoTracking()
                .Where(system => systemIds.Contains(system.Id))
                .OrderBy(system => system.Name)
                .ThenBy(system => system.Id)
                .Select(system => new ExplorerLookupOption(system.Id, system.Name))
                .ToListAsync(cancellationToken);

        var eventTypes = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId)
            .Select(history => history.EventType)
            .Where(eventType => !string.IsNullOrWhiteSpace(eventType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(eventType => eventType, StringComparer.OrdinalIgnoreCase)
            .ToListAsync(cancellationToken);

        return new ExplorerTimelineOptions(races, empires, colonies, systems, worlds, eventTypes);
    }

    public async Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == request.SectorId);

        if (request.RaceId != NoFilterId)
        {
            query = query.Where(history => history.RaceId == request.RaceId || history.OtherRaceId == request.RaceId);
        }

        if (request.EmpireId != NoFilterId)
        {
            query = query.Where(history => history.EmpireId == request.EmpireId);
        }

        if (request.ColonyId != NoFilterId)
        {
            query = query.Where(history => history.ColonyId == request.ColonyId);
        }

        if (request.SystemId != NoFilterId)
        {
            query = query.Where(history => history.StarSystemId == request.SystemId);
        }

        if (request.WorldId != NoFilterId)
        {
            query = query.Where(history => history.PlanetId == request.WorldId);
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query = query.Where(history => history.EventType == request.EventType);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var search = request.Query.Trim();
            query = query.Where(history =>
                history.EventType.Contains(search) ||
                history.Description.Contains(search));
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

        var history = await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(item => EF.Property<int>(item, "Id") == eventId)
            .Select(item => new
            {
                EventId = EF.Property<int>(item, "Id"),
                item.Century,
                item.EventType,
                item.Description,
                item.RaceId,
                item.OtherRaceId,
                item.EmpireId,
                item.ColonyId,
                item.PlanetId,
                item.StarSystemId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (history is null)
        {
            return null;
        }

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
            race,
            otherRace,
            empire,
            colony,
            world,
            system);
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
}
