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
}
