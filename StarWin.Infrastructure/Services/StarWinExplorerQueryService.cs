using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerQueryService(
    IDbContextFactory<StarWinDbContext> dbContextFactory,
    ILogger<StarWinExplorerQueryService>? logger = null) : IStarWinExplorerQueryService
{
    private static readonly TimeSpan SectorEntityUsageCacheDuration = TimeSpan.FromSeconds(20);
    private readonly Dictionary<int, CachedSectorEntityUsage> sectorEntityUsageCache = [];
    private readonly ILogger<StarWinExplorerQueryService> logger = logger ?? NullLogger<StarWinExplorerQueryService>.Instance;

    public async Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return new ExplorerSectorOverviewData(0, 0, 0, 0, 0, 0);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var overviewRows = await dbContext.Database
            .SqlQuery<ExplorerSectorOverviewRow>($"""
                WITH SectorSystems AS (
                    SELECT Id, AllegianceId
                    FROM StarSystems
                    WHERE SectorId = {sectorId}
                ),
                SectorWorlds AS (
                    SELECT Worlds.Id, Worlds.AlienRaceId, Worlds.ControlledByEmpireId, Worlds.AllegianceId
                    FROM Worlds
                    INNER JOIN SectorSystems ON Worlds.StarSystemId = SectorSystems.Id
                ),
                SectorColonies AS (
                    SELECT Colonies.Id, Colonies.RaceId, Colonies.ControllingEmpireId, Colonies.FoundingEmpireId, Colonies.ParentEmpireId, Colonies.AllegianceId
                    FROM Colonies
                    INNER JOIN SectorWorlds ON Colonies.WorldId = SectorWorlds.Id
                ),
                SectorEmpireIds AS (
                    SELECT CAST(AllegianceId AS INTEGER) AS EmpireId
                    FROM SectorSystems
                    WHERE AllegianceId <> {ushort.MaxValue}
                    UNION
                    SELECT CAST(ControlledByEmpireId AS INTEGER) AS EmpireId
                    FROM SectorWorlds
                    WHERE ControlledByEmpireId IS NOT NULL
                    UNION
                    SELECT CAST(AllegianceId AS INTEGER) AS EmpireId
                    FROM SectorWorlds
                    WHERE AllegianceId <> {ushort.MaxValue}
                    UNION
                    SELECT CAST(ControllingEmpireId AS INTEGER) AS EmpireId
                    FROM SectorColonies
                    WHERE ControllingEmpireId IS NOT NULL
                    UNION
                    SELECT CAST(FoundingEmpireId AS INTEGER) AS EmpireId
                    FROM SectorColonies
                    WHERE FoundingEmpireId IS NOT NULL
                    UNION
                    SELECT CAST(ParentEmpireId AS INTEGER) AS EmpireId
                    FROM SectorColonies
                    WHERE ParentEmpireId IS NOT NULL
                    UNION
                    SELECT CAST(AllegianceId AS INTEGER) AS EmpireId
                    FROM SectorColonies
                    WHERE AllegianceId <> {ushort.MaxValue}
                ),
                SectorRaceIds AS (
                    SELECT CAST(AlienRaceId AS INTEGER) AS RaceId
                    FROM SectorWorlds
                    WHERE AlienRaceId IS NOT NULL
                    UNION
                    SELECT CAST(RaceId AS INTEGER) AS RaceId
                    FROM SectorColonies
                )
                SELECT
                    {sectorId} AS SectorId,
                    (SELECT COUNT(*) FROM SectorSystems) AS SystemCount,
                    (SELECT COUNT(*) FROM SectorWorlds) AS WorldCount,
                    (SELECT COUNT(*) FROM SectorColonies) AS ColonyCount,
                    (SELECT COUNT(*) FROM SectorEmpireIds) AS EmpireCount,
                    (SELECT COUNT(*) FROM SectorRaceIds) AS RaceCount
                """)
            .ToListAsync(cancellationToken);
        var overviewRow = overviewRows.Single();

        return new ExplorerSectorOverviewData(
            overviewRow.SectorId,
            overviewRow.SystemCount,
            overviewRow.WorldCount,
            overviewRow.ColonyCount,
            overviewRow.EmpireCount,
            overviewRow.RaceCount);
    }

    public async Task<ExplorerSectorEntityUsage> LoadSectorEntityUsageAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return new ExplorerSectorEntityUsage(0, [], []);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).Value;
    }

    public async Task<IReadOnlyList<StarWinSearchResult>> SearchSectorAsync(
        int sectorId,
        string query,
        int maxResults = 30,
        CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0 || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = query.Trim();
        var searchPattern = $"%{normalizedQuery}%";
        var hasNumericQuery = int.TryParse(normalizedQuery, out var numericQuery);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEntityUsage = await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken);

        var results = new List<StarWinSearchResult>();

        results.AddRange(await dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == sectorId
                && (EF.Functions.Like(system.Name, searchPattern)
                    || (hasNumericQuery && (system.Id == numericQuery || system.LegacySystemId == numericQuery))))
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .Select(system => new StarWinSearchResult
            {
                Type = StarWinSearchResultType.StarSystem,
                Title = system.Name,
                Subtitle = $"System {system.Id} at {DisplayCoordinates(system.Coordinates)}",
                Tab = "Systems",
                SectorId = sectorId,
                SystemId = system.Id
            })
            .Take(maxResults)
            .ToListAsync(cancellationToken));

        results.AddRange(await (
            from world in dbContext.Worlds.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && (EF.Functions.Like(world.Name, searchPattern)
                    || EF.Functions.Like(world.WorldType, searchPattern)
                    || (hasNumericQuery && (world.Id == numericQuery
                        || world.LegacyPlanetId == numericQuery
                        || world.LegacyMoonId == numericQuery)))
            orderby world.Name, world.Id
            select new StarWinSearchResult
            {
                Type = StarWinSearchResultType.World,
                Title = world.Name,
                Subtitle = $"{world.Kind} {world.Id} in {system.Name}; {world.WorldType}, {world.AtmosphereType}",
                Tab = "Worlds",
                SectorId = sectorId,
                SystemId = system.Id,
                WorldId = world.Id
            })
            .Take(maxResults)
            .ToListAsync(cancellationToken));

        results.AddRange(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && (EF.Functions.Like(colony.ColonyClass, searchPattern)
                    || EF.Functions.Like(colony.ColonistRaceName, searchPattern)
                    || EF.Functions.Like(colony.AllegianceName, searchPattern)
                    || EF.Functions.Like(colony.Name, searchPattern)
                    || (hasNumericQuery && (colony.Id == numericQuery || colony.WorldId == numericQuery)))
            orderby world.Name, colony.Id
            select new StarWinSearchResult
            {
                Type = StarWinSearchResultType.Colony,
                Title = $"{(string.IsNullOrWhiteSpace(colony.Name) ? colony.ColonyClass : colony.Name)} on {world.Name}",
                Subtitle = $"{colony.ColonistRaceName}; {colony.AllegianceName}; {DisplayPopulation(colony.EstimatedPopulation)}",
                Tab = "Colonies",
                SectorId = sectorId,
                SystemId = system.Id,
                WorldId = world.Id,
                ColonyId = colony.Id,
                RaceId = colony.RaceId,
                EmpireId = colony.ControllingEmpireId
            })
            .Take(maxResults)
            .ToListAsync(cancellationToken));

        results.AddRange(await (
            from habitat in dbContext.SpaceHabitats.AsNoTracking()
            join system in dbContext.StarSystems.AsNoTracking()
                on EF.Property<int>(habitat, "StarSystemId") equals system.Id
            where system.SectorId == sectorId
                && (EF.Functions.Like(habitat.Name, searchPattern)
                    || (hasNumericQuery && habitat.Id == numericQuery))
            orderby habitat.Name, habitat.Id
            select new StarWinSearchResult
            {
                Type = StarWinSearchResultType.SpaceHabitat,
                Title = habitat.Name,
                Subtitle = $"Habitat in {system.Name}; population {DisplayPopulation(habitat.Population)}",
                Tab = "Worlds",
                SectorId = sectorId,
                SystemId = system.Id,
                SpaceHabitatId = habitat.Id,
                EmpireId = habitat.ControlledByEmpireId
            })
            .Take(maxResults)
            .ToListAsync(cancellationToken));

        results.AddRange(await dbContext.HistoryEvents
            .AsNoTracking()
            .Where(history => history.SectorId == sectorId
                && (EF.Functions.Like(history.EventType, searchPattern)
                    || EF.Functions.Like(history.Description, searchPattern)
                    || (hasNumericQuery && history.Century == numericQuery)))
            .OrderBy(history => history.Century)
            .ThenBy(history => history.EventType)
            .Select(history => new StarWinSearchResult
            {
                Type = StarWinSearchResultType.History,
                Title = $"{history.EventType} C{history.Century}",
                Subtitle = history.Description,
                Tab = "Timeline",
                SectorId = sectorId,
                SystemId = history.StarSystemId,
                WorldId = history.PlanetId,
                RaceId = history.RaceId,
                EmpireId = history.EmpireId
            })
            .Take(maxResults)
            .ToListAsync(cancellationToken));

        if (sectorEntityUsage.EmpireIds.Count > 0)
        {
            var sectorEmpireIds = sectorEntityUsage.EmpireIds;
            results.AddRange(await dbContext.Empires
                .AsNoTracking()
                .Where(empire => sectorEmpireIds.Contains(empire.Id)
                    && (EF.Functions.Like(empire.Name, searchPattern)
                        || (hasNumericQuery && (empire.Id == numericQuery || empire.CivilizationProfile.TechLevel == numericQuery))))
                .OrderBy(empire => empire.Name)
                .ThenBy(empire => empire.Id)
                .Select(empire => new StarWinSearchResult
                {
                    Type = StarWinSearchResultType.Empire,
                    Title = empire.Name,
                    Subtitle = $"TL {empire.CivilizationProfile.TechLevel}; {empire.Planets} planets; {empire.MilitaryPower} military power",
                    Tab = "Empires",
                    SectorId = sectorId,
                    EmpireId = empire.Id,
                    RaceId = empire.LegacyRaceId
                })
                .Take(maxResults)
                .ToListAsync(cancellationToken));
        }

        if (sectorEntityUsage.RaceIds.Count > 0)
        {
            var sectorRaceIds = sectorEntityUsage.RaceIds;
            results.AddRange(await dbContext.AlienRaces
                .AsNoTracking()
                .Where(race => sectorRaceIds.Contains(race.Id)
                    && (EF.Functions.Like(race.Name, searchPattern)
                        || EF.Functions.Like(race.AppearanceType, searchPattern)
                        || EF.Functions.Like(race.EnvironmentType, searchPattern)
                        || EF.Functions.Like(race.BodyChemistry, searchPattern)
                        || (hasNumericQuery && race.Id == numericQuery)))
                .OrderBy(race => race.Name)
                .ThenBy(race => race.Id)
                .Select(race => new StarWinSearchResult
                {
                    Type = StarWinSearchResultType.AlienRace,
                    Title = race.Name,
                    Subtitle = $"{race.AppearanceType}; {race.EnvironmentType}; {race.BodyChemistry} biology",
                    Tab = "Aliens",
                    SectorId = sectorId,
                    RaceId = race.Id
                })
                .Take(maxResults)
                .ToListAsync(cancellationToken));
        }

        return results
            .OrderBy(result => RankSearchResult(result, normalizedQuery))
            .ThenBy(result => result.Title, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    public async Task<int?> ResolveSystemIdAsync(
        int sectorId,
        int? worldId = null,
        int? colonyId = null,
        int? habitatId = null,
        CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (colonyId is int requestedColonyId && requestedColonyId > 0)
        {
            return await (
                from colony in dbContext.Colonies.AsNoTracking()
                join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
                join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
                where system.SectorId == sectorId && colony.Id == requestedColonyId
                select (int?)system.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (habitatId is int requestedHabitatId && requestedHabitatId > 0)
        {
            return await (
                from habitat in dbContext.SpaceHabitats.AsNoTracking()
                join system in dbContext.StarSystems.AsNoTracking()
                    on EF.Property<int>(habitat, "StarSystemId") equals system.Id
                where system.SectorId == sectorId && habitat.Id == requestedHabitatId
                select (int?)system.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (worldId is int requestedWorldId && requestedWorldId > 0)
        {
            return await (
                from world in dbContext.Worlds.AsNoTracking()
                join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
                where system.SectorId == sectorId && world.Id == requestedWorldId
                select (int?)system.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyList<ExplorerLookupOption>> LoadSectorEmpireOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEntityUsage = await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken);
        if (sectorEntityUsage.EmpireIds.Count == 0)
        {
            return [];
        }

        var sectorEmpireIds = sectorEntityUsage.EmpireIds;
        return await dbContext.Empires
            .AsNoTracking()
            .Where(empire => sectorEmpireIds.Contains(empire.Id))
            .OrderBy(empire => empire.Name)
            .ThenBy(empire => empire.Id)
            .Select(empire => new ExplorerLookupOption(empire.Id, empire.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExplorerSystemFilterOptions> LoadSystemFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        return new ExplorerSystemFilterOptions(await LoadSectorEmpireOptionsAsync(sectorId, cancellationToken));
    }

    public async Task<ExplorerSystemListPage> LoadSystemListPageAsync(ExplorerSystemListPageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var systemsQuery = dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == request.SectorId);

        if (request.EmpireId is int empireId)
        {
            systemsQuery = systemsQuery.Where(system => system.AllegianceId == empireId);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var normalizedQuery = request.Query.Trim();
            var searchPattern = $"%{normalizedQuery}%";
            var hasNumericQuery = int.TryParse(normalizedQuery, out var numericQuery);
            systemsQuery = systemsQuery.Where(system =>
                EF.Functions.Like(system.Name, searchPattern)
                || (hasNumericQuery && (system.Id == numericQuery || system.LegacySystemId == numericQuery))
                || system.AstralBodies.Any(body =>
                    EF.Functions.Like(body.Classification, searchPattern)
                    || EF.Functions.Like(body.Role.ToString(), searchPattern)
                    || EF.Functions.Like(body.Kind.ToString(), searchPattern))
                || system.Worlds.Any(world =>
                    EF.Functions.Like(world.Name, searchPattern)
                    || EF.Functions.Like(world.WorldType, searchPattern))
                || (system.AllegianceId != ushort.MaxValue
                    && dbContext.Empires.Any(empire => empire.Id == system.AllegianceId && EF.Functions.Like(empire.Name, searchPattern))));
        }

        var items = await systemsQuery
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .Select(system => new ExplorerSystemListItem(
                system.Id,
                system.LegacySystemId,
                system.Name,
                system.Coordinates,
                system.AllegianceId,
                system.AllegianceId == ushort.MaxValue
                    ? "Independent"
                    : dbContext.Empires
                        .Where(empire => empire.Id == system.AllegianceId)
                        .Select(empire => empire.Name)
                        .FirstOrDefault() ?? system.AllegianceId.ToString()))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > request.Limit;
        return new ExplorerSystemListPage(items.Take(request.Limit).ToList(), hasMore);
    }

    public async Task<ExplorerSystemListItem?> LoadSystemListItemAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == sectorId && system.Id == systemId)
            .Select(system => new ExplorerSystemListItem(
                system.Id,
                system.LegacySystemId,
                system.Name,
                system.Coordinates,
                system.AllegianceId,
                system.AllegianceId == ushort.MaxValue
                    ? "Independent"
                    : dbContext.Empires
                        .Where(empire => empire.Id == system.AllegianceId)
                        .Select(empire => empire.Name)
                        .FirstOrDefault() ?? system.AllegianceId.ToString()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExplorerSystemDetail?> LoadSystemDetailAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
    {
        var system = await LoadProjectedSystemDetailAsync(
            sectorId,
            systemId,
            includeWorldCharacteristics: false,
            includeColonies: false,
            includeColonyDemographics: false,
            cancellationToken: cancellationToken);

        return system is null ? null : new ExplorerSystemDetail(sectorId, system);
    }

    public async Task<ExplorerWorldsWorkspace?> LoadWorldsWorkspaceAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
    {
        var system = await LoadProjectedSystemDetailAsync(
            sectorId,
            systemId,
            includeWorldCharacteristics: true,
            includeColonies: true,
            includeColonyDemographics: true,
            cancellationToken: cancellationToken);
        if (system is null)
        {
            return null;
        }

        var empires = await LoadSectorEmpireOptionsAsync(sectorId, cancellationToken);
        return new ExplorerWorldsWorkspace(sectorId, system, empires);
    }

    public async Task<ExplorerColonyFilterOptions> LoadColonyFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEntityUsage = await LoadSectorEntityUsageAsync(sectorId, cancellationToken);
        var raceIds = sectorEntityUsage.RaceIds.ToHashSet();
        var empireIds = sectorEntityUsage.EmpireIds.ToHashSet();

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

        var classes = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && !string.IsNullOrWhiteSpace(colony.ColonyClass)
            orderby colony.ColonyClass
            select colony.ColonyClass)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new ExplorerColonyFilterOptions(races, empires, classes);
    }

    public async Task<ExplorerColonyListPage> LoadColonyListPageAsync(ExplorerColonyListPageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = from colony in dbContext.Colonies.AsNoTracking()
                    join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
                    join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
                    where system.SectorId == request.SectorId
                    select new
                    {
                        Colony = colony,
                        World = world
                    };

        if (request.RaceId is int raceId)
        {
            query = query.Where(item =>
                item.Colony.RaceId == raceId
                || dbContext.Set<ColonyDemographic>().Any(demographic => demographic.ColonyId == item.Colony.Id && demographic.RaceId == raceId));
        }

        if (request.EmpireId is int empireId)
        {
            query = query.Where(item =>
                item.Colony.AllegianceId == empireId
                || item.Colony.ControllingEmpireId == empireId
                || item.Colony.FoundingEmpireId == empireId
                || item.Colony.ParentEmpireId == empireId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<ColonyPoliticalStatus>(request.Status.Trim(), true, out var politicalStatus))
        {
            query = query.Where(item => item.Colony.PoliticalStatus == politicalStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.ColonyClass))
        {
            var colonyClass = request.ColonyClass.Trim();
            query = query.Where(item => item.Colony.ColonyClass == colonyClass);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var normalizedQuery = request.Query.Trim();
            var searchPattern = $"%{normalizedQuery}%";
            var hasNumericQuery = int.TryParse(normalizedQuery, out var numericQuery);
            query = query.Where(item =>
                EF.Functions.Like(item.World.Name, searchPattern)
                || EF.Functions.Like(item.World.WorldType, searchPattern)
                || EF.Functions.Like(item.Colony.Name, searchPattern)
                || EF.Functions.Like(item.Colony.ColonyClass, searchPattern)
                || EF.Functions.Like(item.Colony.ColonistRaceName, searchPattern)
                || EF.Functions.Like(item.Colony.AllegianceName, searchPattern)
                || EF.Functions.Like(item.Colony.Starport, searchPattern)
                || EF.Functions.Like(item.Colony.GovernmentType, searchPattern)
                || EF.Functions.Like(item.Colony.ExportResource, searchPattern)
                || EF.Functions.Like(item.Colony.ImportResource, searchPattern)
                || dbContext.Set<ColonyDemographic>().Any(demographic => demographic.ColonyId == item.Colony.Id && EF.Functions.Like(demographic.RaceName, searchPattern))
                || (hasNumericQuery && (item.Colony.Id == numericQuery || item.Colony.WorldId == numericQuery)));
        }

        var items = await query
            .OrderByDescending(item => item.Colony.EstimatedPopulation)
            .ThenBy(item => item.World.Name)
            .ThenBy(item => item.Colony.Id)
            .Select(item => new ExplorerColonyListItem(
                item.Colony.Id,
                item.World.Id,
                item.World.StarSystemId ?? 0,
                string.IsNullOrWhiteSpace(item.Colony.Name) ? item.Colony.ColonyClass : item.Colony.Name,
                item.World.Name,
                item.Colony.AllegianceName,
                item.Colony.EstimatedPopulation))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > request.Limit;
        return new ExplorerColonyListPage(items.Take(request.Limit).ToList(), hasMore);
    }

    public async Task<ExplorerColonyListItem?> LoadColonyListItemAsync(int sectorId, int colonyId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId && colony.Id == colonyId
            select new ExplorerColonyListItem(
                colony.Id,
                world.Id,
                world.StarSystemId ?? 0,
                string.IsNullOrWhiteSpace(colony.Name) ? colony.ColonyClass : colony.Name,
                world.Name,
                colony.AllegianceName,
                colony.EstimatedPopulation))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExplorerColonyDetail?> LoadColonyDetailAsync(int sectorId, int colonyId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var detailRow = await (
            from colonyRow in dbContext.Colonies.AsNoTracking()
            join worldRow in dbContext.Worlds.AsNoTracking() on colonyRow.WorldId equals worldRow.Id
            join systemRow in dbContext.StarSystems.AsNoTracking() on worldRow.StarSystemId equals systemRow.Id
            where systemRow.SectorId == sectorId && colonyRow.Id == colonyId
            select new
            {
                Colony = colonyRow,
                World = worldRow
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (detailRow is null)
        {
            return null;
        }

        var demographics = await dbContext.Set<ColonyDemographic>()
            .AsNoTracking()
            .Where(demographic => demographic.ColonyId == colonyId)
            .OrderByDescending(demographic => demographic.PopulationPercent)
            .ToListAsync(cancellationToken);

        var colony = CopyColony(detailRow.Colony, includeDemographics: false);
        foreach (var demographic in demographics)
        {
            colony.Demographics.Add(new ColonyDemographic
            {
                ColonyId = demographic.ColonyId,
                RaceId = demographic.RaceId,
                RaceName = demographic.RaceName,
                PopulationPercent = demographic.PopulationPercent,
                Population = demographic.Population
            });
        }

        var world = CopyWorld(detailRow.World, includeColony: false, includeCharacteristics: false);
        return new ExplorerColonyDetail(sectorId, colony, world);
    }

    public async Task<ExplorerHyperlaneWorkspace?> LoadHyperlaneWorkspaceAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorRow = await dbContext.Sectors
            .AsNoTracking()
            .Where(sector => sector.Id == sectorId)
            .Select(sector => new
            {
                sector.Id,
                sector.Name,
                sector.Configuration
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (sectorRow is null)
        {
            return null;
        }

        var systems = await dbContext.StarSystems
            .AsNoTracking()
            .Where(system => system.SectorId == sectorId)
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .Select(system => new ExplorerHyperlaneSystem(
                system.Id,
                system.LegacySystemId,
                system.Name,
                system.Coordinates,
                system.AllegianceId))
            .ToListAsync(cancellationToken);

        var savedRoutes = await dbContext.SectorSavedRoutes
            .AsNoTracking()
            .Where(route => route.SectorId == sectorId)
            .OrderBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .Select(route => new SectorSavedRoute
            {
                Id = route.Id,
                SectorId = route.SectorId,
                SourceSystemId = route.SourceSystemId,
                TargetSystemId = route.TargetSystemId,
                DistanceParsecs = route.DistanceParsecs,
                TravelTimeYears = route.TravelTimeYears,
                TechnologyLevel = route.TechnologyLevel,
                TierName = route.TierName,
                PrimaryOwnerEmpireId = route.PrimaryOwnerEmpireId,
                PrimaryOwnerEmpireName = route.PrimaryOwnerEmpireName,
                SecondaryOwnerEmpireId = route.SecondaryOwnerEmpireId,
                SecondaryOwnerEmpireName = route.SecondaryOwnerEmpireName,
                IsUserPersisted = route.IsUserPersisted,
                GeneratedAtUtc = route.GeneratedAtUtc
            })
            .ToListAsync(cancellationToken);

        var eligibleSystemIds = new HashSet<int>();
        eligibleSystemIds.UnionWith(await (
            from system in dbContext.StarSystems.AsNoTracking()
            join empire in dbContext.Empires.AsNoTracking() on (int)system.AllegianceId equals empire.Id
            where system.SectorId == sectorId
                && system.AllegianceId != ushort.MaxValue
                && empire.CivilizationProfile.TechLevel >= 6
            select system.Id)
            .ToListAsync(cancellationToken));
        eligibleSystemIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join empire in dbContext.Empires.AsNoTracking() on colony.ControllingEmpireId equals empire.Id
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && colony.ControllingEmpireId.HasValue
                && empire.CivilizationProfile.TechLevel >= 6
            select system.Id)
            .ToListAsync(cancellationToken));
        eligibleSystemIds.UnionWith(await (
            from colony in dbContext.Colonies.AsNoTracking()
            join empire in dbContext.Empires.AsNoTracking() on (int)colony.AllegianceId equals empire.Id
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && colony.AllegianceId != ushort.MaxValue
                && empire.CivilizationProfile.TechLevel >= 6
            select system.Id)
            .ToListAsync(cancellationToken));

        var empires = await LoadSectorEmpireOptionsAsync(sectorId, cancellationToken);
        return new ExplorerHyperlaneWorkspace(
            sectorRow.Id,
            sectorRow.Name,
            CloneSectorConfiguration(sectorRow.Configuration),
            systems,
            savedRoutes,
            eligibleSystemIds.OrderBy(id => id).ToList(),
            empires);
    }

    public async Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Explorer query start {Operation}. sectorId={SectorId}", nameof(LoadAlienRaceFilterOptionsAsync), sectorId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEntityUsage = await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken);
        var raceIds = sectorEntityUsage.RaceIds;
        if (raceIds.Count == 0)
        {
            logger.LogInformation(
                "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceCount=0",
                nameof(LoadAlienRaceFilterOptionsAsync),
                stopwatch.ElapsedMilliseconds,
                sectorId);
            return new ExplorerAlienRaceFilterOptions([], [], [], []);
        }

        var environmentTypes = await dbContext.AlienRaces
            .AsNoTracking()
            .Where(race => raceIds.Contains(race.Id) && !string.IsNullOrWhiteSpace(race.EnvironmentType))
            .Select(race => race.EnvironmentType)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync(cancellationToken);

        var appearanceTypes = await dbContext.AlienRaces
            .AsNoTracking()
            .Where(race => raceIds.Contains(race.Id) && !string.IsNullOrWhiteSpace(race.AppearanceType))
            .Select(race => race.AppearanceType)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync(cancellationToken);

        var sectorEmpireIds = sectorEntityUsage.EmpireIds;
        var starWinTechLevels = sectorEmpireIds.Count == 0
            ? []
            : await dbContext.Empires
                .AsNoTracking()
                .Where(empire => sectorEmpireIds.Contains(empire.Id))
                .Select(empire => (int)empire.CivilizationProfile.TechLevel)
                .Distinct()
                .OrderBy(value => value)
                .ToListAsync(cancellationToken);

        var superscienceRaceIds = await LoadSuperscienceRaceIdsAsync(dbContext, raceIds, cancellationToken);
        var gurpsTechLevelPairs = sectorEmpireIds.Count == 0
            ? []
            : await (
                from empire in dbContext.Empires.AsNoTracking()
                from membership in empire.RaceMemberships
                where sectorEmpireIds.Contains(empire.Id) && raceIds.Contains(membership.RaceId)
                select new
                {
                    GurpsTechLevel = (int)empire.CivilizationProfile.TechLevel + 2,
                    membership.RaceId
                })
                .Distinct()
                .ToListAsync(cancellationToken);

        var options = new ExplorerAlienRaceFilterOptions(
            environmentTypes,
            appearanceTypes,
            starWinTechLevels,
            gurpsTechLevelPairs
                .Select(value => GurpsTechnologyLevelMapper.FormatDisplay(value.GurpsTechLevel, superscienceRaceIds.Contains(value.RaceId)))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList());

        logger.LogInformation(
            "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceCount={RaceCount} empireCount={EmpireCount} environmentCount={EnvironmentCount} appearanceCount={AppearanceCount} starWinTechLevelCount={StarWinTechLevelCount} gurpsTechLevelCount={GurpsTechLevelCount}",
            nameof(LoadAlienRaceFilterOptionsAsync),
            stopwatch.ElapsedMilliseconds,
            sectorId,
            raceIds.Count,
            sectorEmpireIds.Count,
            options.EnvironmentTypes.Count,
            options.AppearanceTypes.Count,
            options.StarWinTechLevels.Count,
            options.GurpsTechLevels.Count);

        return options;
    }

    public async Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Explorer query start {Operation}. sectorId={SectorId} offset={Offset} limit={Limit} query={Query} environment={EnvironmentType} appearance={AppearanceType} starWinTechLevel={StarWinTechLevel} gurpsTechLevel={GurpsTechLevel} requireSuperscience={RequireSuperscience}",
            nameof(LoadAlienRaceListPageAsync),
            request.SectorId,
            request.Offset,
            request.Limit,
            request.Query,
            request.EnvironmentType,
            request.AppearanceType,
            request.StarWinTechLevel,
            request.GurpsTechLevel,
            request.RequireSuperscience);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEntityUsage = await GetCachedSectorEntityUsageAsync(dbContext, request.SectorId, cancellationToken);
        var raceIds = sectorEntityUsage.RaceIds;
        if (raceIds.Count == 0)
        {
            logger.LogInformation(
                "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceCount=0",
                nameof(LoadAlienRaceListPageAsync),
                stopwatch.ElapsedMilliseconds,
                request.SectorId);
            return new ExplorerAlienRaceListPage([], false);
        }

        var sectorEmpireIds = sectorEntityUsage.EmpireIds;
        var racesQuery = dbContext.AlienRaces
            .AsNoTracking()
            .Where(race => raceIds.Contains(race.Id));

        racesQuery = ApplyAlienRaceFilters(dbContext, racesQuery, sectorEmpireIds, request);

        var races = await racesQuery
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
        var page = new ExplorerAlienRaceListPage(races.Take(request.Limit).ToList(), hasMore);
        logger.LogInformation(
            "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceCount={RaceCount} returnedCount={ReturnedCount} hasMore={HasMore}",
            nameof(LoadAlienRaceListPageAsync),
            stopwatch.ElapsedMilliseconds,
            request.SectorId,
            raceIds.Count,
            page.Items.Count,
            page.HasMore);

        return page;
    }

    public async Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var raceIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).RaceIds;
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
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Explorer query start {Operation}. sectorId={SectorId} raceId={RaceId}",
            nameof(LoadAlienRaceDetailAsync),
            sectorId,
            raceId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEntityUsage = await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken);
        var raceIds = sectorEntityUsage.RaceIds;
        if (!raceIds.Contains(raceId))
        {
            logger.LogInformation(
                "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceId={RaceId} foundInSector=false",
                nameof(LoadAlienRaceDetailAsync),
                stopwatch.ElapsedMilliseconds,
                sectorId,
                raceId);
            return null;
        }

        var race = await dbContext.AlienRaces
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == raceId, cancellationToken);
        if (race is null)
        {
            logger.LogInformation(
                "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceId={RaceId} raceMissing=true",
                nameof(LoadAlienRaceDetailAsync),
                stopwatch.ElapsedMilliseconds,
                sectorId,
                raceId);
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

        var sectorEmpireIdSet = sectorEntityUsage.EmpireIds.ToHashSet();
        var matchingMemberships = sectorEmpireIdSet.Count == 0
            ? []
            : (await dbContext.Set<EmpireRaceMembership>()
                .AsNoTracking()
                .Where(membership => membership.RaceId == raceId)
                .Select(membership => new EmpireRaceMembership
                {
                    EmpireId = membership.EmpireId,
                    RaceId = membership.RaceId,
                    Role = membership.Role,
                    PopulationMillions = membership.PopulationMillions,
                    IsPrimary = membership.IsPrimary
                })
                .ToListAsync(cancellationToken))
                .Where(membership => sectorEmpireIdSet.Contains(membership.EmpireId))
                .OrderByDescending(membership => membership.IsPrimary)
                .ThenBy(membership => membership.EmpireId)
                .ToList();

        var matchingEmpireIds = matchingMemberships
            .Select(membership => membership.EmpireId)
            .Distinct()
            .ToList();
        var empireRows = matchingEmpireIds.Count == 0
            ? []
            : await dbContext.Empires
                .AsNoTracking()
                .Where(empire => matchingEmpireIds.Contains(empire.Id))
                .OrderBy(empire => empire.Name)
                .ThenBy(empire => empire.Id)
                .Select(empire => new AlienRaceEmpireDetailProjection(
                    empire.Id,
                    empire.Name,
                    empire.GovernmentType,
                    empire.CivilizationProfile.Militancy,
                    empire.CivilizationProfile.Determination,
                    empire.CivilizationProfile.RacialTolerance,
                    empire.CivilizationProfile.Progressiveness,
                    empire.CivilizationProfile.Loyalty,
                    empire.CivilizationProfile.SocialCohesion,
                    empire.CivilizationProfile.TechLevel,
                    empire.CivilizationProfile.Art,
                    empire.CivilizationProfile.Individualism,
                    empire.CivilizationProfile.SpatialAge,
                    empire.CivilizationModifiers.Militancy,
                    empire.CivilizationModifiers.Determination,
                    empire.CivilizationModifiers.RacialTolerance,
                    empire.CivilizationModifiers.Progressiveness,
                    empire.CivilizationModifiers.Loyalty,
                    empire.CivilizationModifiers.SocialCohesion,
                    empire.CivilizationModifiers.Art,
                    empire.CivilizationModifiers.Individualism))
                .ToListAsync(cancellationToken);
        var religionRows = matchingEmpireIds.Count == 0
            ? []
            : await dbContext.Set<EmpireReligion>()
                .AsNoTracking()
                .Where(religion => matchingEmpireIds.Contains(religion.EmpireId))
                .OrderBy(religion => religion.EmpireId)
                .ThenByDescending(religion => religion.PopulationPercent)
                .ThenBy(religion => religion.ReligionName)
                .Select(religion => new EmpireReligion
                {
                    EmpireId = religion.EmpireId,
                    ReligionId = religion.ReligionId,
                    ReligionName = religion.ReligionName,
                    PopulationPercent = religion.PopulationPercent
                })
                .ToListAsync(cancellationToken);

        var membershipsByEmpireId = matchingMemberships
            .GroupBy(membership => membership.EmpireId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<EmpireRaceMembership>)group.ToList());
        var religionsByEmpireId = religionRows
            .GroupBy(religion => religion.EmpireId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<EmpireReligion>)group.ToList());
        var empires = empireRows
            .Select(row => BuildAlienRaceDetailEmpire(row, membershipsByEmpireId, religionsByEmpireId))
            .ToList();

        var detail = new ExplorerAlienRaceDetail(sectorId, race, homeWorld, empires);
        logger.LogInformation(
            "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceId={RaceId} empireCount={EmpireCount} homeWorldId={HomeWorldId}",
            nameof(LoadAlienRaceDetailAsync),
            stopwatch.ElapsedMilliseconds,
            sectorId,
            raceId,
            empires.Count,
            homeWorld?.Id ?? 0);

        return detail;
    }

    private static Empire BuildAlienRaceDetailEmpire(
        AlienRaceEmpireDetailProjection projection,
        IReadOnlyDictionary<int, IReadOnlyList<EmpireRaceMembership>> membershipsByEmpireId,
        IReadOnlyDictionary<int, IReadOnlyList<EmpireReligion>> religionsByEmpireId)
    {
        var empire = new Empire
        {
            Id = projection.EmpireId,
            Name = projection.Name,
            GovernmentType = projection.GovernmentType,
            CivilizationProfile = new CivilizationProfile
            {
                Militancy = projection.Militancy,
                Determination = projection.Determination,
                RacialTolerance = projection.RacialTolerance,
                Progressiveness = projection.Progressiveness,
                Loyalty = projection.Loyalty,
                SocialCohesion = projection.SocialCohesion,
                TechLevel = projection.TechLevel,
                Art = projection.Art,
                Individualism = projection.Individualism,
                SpatialAge = projection.SpatialAge
            },
            CivilizationModifiers = new CivilizationModifierProfile
            {
                Militancy = projection.MilitancyModifier,
                Determination = projection.DeterminationModifier,
                RacialTolerance = projection.RacialToleranceModifier,
                Progressiveness = projection.ProgressivenessModifier,
                Loyalty = projection.LoyaltyModifier,
                SocialCohesion = projection.SocialCohesionModifier,
                Art = projection.ArtModifier,
                Individualism = projection.IndividualismModifier
            }
        };

        if (membershipsByEmpireId.TryGetValue(projection.EmpireId, out var memberships))
        {
            foreach (var membership in memberships)
            {
                empire.RaceMemberships.Add(membership);
            }
        }

        if (religionsByEmpireId.TryGetValue(projection.EmpireId, out var religions))
        {
            foreach (var religion in religions)
            {
                empire.Religions.Add(religion);
            }
        }

        return empire;
    }

    public async Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).EmpireIds;
        if (sectorEmpireIds.Count == 0)
        {
            return new ExplorerEmpireFilterOptions([]);
        }

        var raceOptions = await (
            from empire in dbContext.Empires.AsNoTracking()
            from membership in empire.RaceMemberships
            join race in dbContext.AlienRaces.AsNoTracking() on membership.RaceId equals race.Id
            join homeWorld in dbContext.Worlds.AsNoTracking() on race.HomePlanetId equals homeWorld.Id into homeWorlds
            from homeWorld in homeWorlds.DefaultIfEmpty()
            where sectorEmpireIds.Contains(empire.Id)
            orderby race.Name, race.Id
            select new RaceDisplayProjection(
                race.Id,
                race.Name,
                homeWorld != null ? homeWorld.Name : null))
            .Distinct()
            .ToListAsync(cancellationToken);

        return new ExplorerEmpireFilterOptions(raceOptions
            .Select(race => new ExplorerLookupOption(
                race.RaceId,
                ResolveRaceDisplayName(race.RaceId, race.RaceName, race.HomeWorldName)))
            .DistinctBy(race => race.Id)
            .OrderBy(race => race.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(race => race.Id)
            .ToList());
    }

    public async Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, request.SectorId, cancellationToken)).EmpireIds;
        if (sectorEmpireIds.Count == 0)
        {
            return new ExplorerEmpireListPage([], false);
        }

        var empiresQuery = dbContext.Empires
            .AsNoTracking()
            .Where(empire => sectorEmpireIds.Contains(empire.Id));

        empiresQuery = ApplyEmpireFilters(dbContext, empiresQuery, request);

        var headerItems = await empiresQuery
            .OrderBy(empire => empire.Name)
            .ThenBy(empire => empire.Id)
            .Select(empire => new EmpireListHeaderProjection(
                empire.Id,
                empire.Name,
                empire.CivilizationProfile.TechLevel + 2,
                empire.GovernmentType,
                empire.Founding.Origin,
                empire.Founding.FoundingRaceId,
                empire.Founding.FoundingWorldId,
                dbContext.AlienRaces
                    .Where(race => race.Id == empire.Founding.FoundingRaceId)
                    .Select(race => race.Name)
                    .FirstOrDefault(),
                dbContext.Worlds
                    .Where(world => world.Id == empire.Founding.FoundingWorldId)
                    .Select(world => world.Name)
                    .FirstOrDefault(),
                empire.IsFallen))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = headerItems.Count > request.Limit;
        var visibleHeaders = headerItems.Take(request.Limit).ToList();
        var worldCountsByEmpireId = await LoadEmpireWorldCountsByEmpireIdAsync(
            dbContext,
            request.SectorId,
            visibleHeaders.Select(item => item.EmpireId).ToList(),
            cancellationToken);
        return new ExplorerEmpireListPage(
            visibleHeaders
                .Select(item =>
                {
                    var counts = worldCountsByEmpireId.GetValueOrDefault(item.EmpireId) ?? EmptyEmpireWorldCountProjection;
                    return BuildEmpireListItem(new EmpireListProjection(
                        item.EmpireId,
                        item.Name,
                        counts.ControlledWorldCount,
                        counts.TrackedWorldCount,
                        item.GurpsTechLevel,
                        item.GovernmentType,
                        item.Origin,
                        item.FoundingRaceId,
                        item.FoundingWorldId,
                        item.FoundingRaceName,
                        item.FoundingWorldName,
                        item.IsFallen));
                })
                .ToList(),
            hasMore);
    }

    public async Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).EmpireIds;
        if (!sectorEmpireIds.Contains(empireId))
        {
            return null;
        }

        var item = await dbContext.Empires
            .AsNoTracking()
            .Where(empire => empire.Id == empireId)
            .Select(empire => new EmpireListHeaderProjection(
                empire.Id,
                empire.Name,
                empire.CivilizationProfile.TechLevel + 2,
                empire.GovernmentType,
                empire.Founding.Origin,
                empire.Founding.FoundingRaceId,
                empire.Founding.FoundingWorldId,
                dbContext.AlienRaces
                    .Where(race => race.Id == empire.Founding.FoundingRaceId)
                    .Select(race => race.Name)
                    .FirstOrDefault(),
                dbContext.Worlds
                    .Where(world => world.Id == empire.Founding.FoundingWorldId)
                    .Select(world => world.Name)
                    .FirstOrDefault(),
                empire.IsFallen))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        var counts = (await LoadEmpireWorldCountsByEmpireIdAsync(
            dbContext,
            sectorId,
            [item.EmpireId],
            cancellationToken))
            .GetValueOrDefault(item.EmpireId)
            ?? EmptyEmpireWorldCountProjection;
        return BuildEmpireListItem(new EmpireListProjection(
            item.EmpireId,
            item.Name,
            counts.ControlledWorldCount,
            counts.TrackedWorldCount,
            item.GurpsTechLevel,
            item.GovernmentType,
            item.Origin,
            item.FoundingRaceId,
            item.FoundingWorldId,
            item.FoundingRaceName,
            item.FoundingWorldName,
            item.IsFallen));
    }

    public async Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).EmpireIds;
        if (!sectorEmpireIds.Contains(empireId))
        {
            return null;
        }

        var empire = await dbContext.Empires
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.RaceMemberships)
            .Include(item => item.Contacts)
            .Include(item => item.Religions)
            .FirstOrDefaultAsync(item => item.Id == empireId, cancellationToken);
        if (empire is null)
        {
            return null;
        }

        var homeWorld = empire.Founding.FoundingWorldId is int worldId
            ? await dbContext.Worlds
                .AsNoTracking()
                .Where(world => world.Id == worldId)
                .Select(world => new StarWin.Domain.Model.Entity.StarMap.World
                {
                    Id = world.Id,
                    Name = world.Name,
                    WorldType = world.WorldType,
                    StarSystemId = world.StarSystemId
                })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var memberRaceRows = await (
            from membership in dbContext.Set<EmpireRaceMembership>().AsNoTracking()
            join race in dbContext.AlienRaces.AsNoTracking() on membership.RaceId equals race.Id
            join raceHomeWorld in dbContext.Worlds.AsNoTracking() on race.HomePlanetId equals raceHomeWorld.Id into raceHomeWorlds
            from raceHomeWorld in raceHomeWorlds.DefaultIfEmpty()
            where membership.EmpireId == empireId
            orderby membership.IsPrimary descending, race.Name, race.Id
            select new EmpireRaceMembershipProjection(
                membership.RaceId,
                race.Name,
                raceHomeWorld != null ? raceHomeWorld.Name : null,
                membership.Role,
                membership.PopulationMillions,
                membership.IsPrimary))
            .ToListAsync(cancellationToken);

        var controlledDemographicRows = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            join demographic in dbContext.Set<ColonyDemographic>().AsNoTracking() on colony.Id equals demographic.ColonyId
            join race in dbContext.AlienRaces.AsNoTracking() on demographic.RaceId equals race.Id into races
            from race in races.DefaultIfEmpty()
            join raceHomeWorld in dbContext.Worlds.AsNoTracking() on race.HomePlanetId equals raceHomeWorld.Id into raceHomeWorlds
            from raceHomeWorld in raceHomeWorlds.DefaultIfEmpty()
            where system.SectorId == sectorId
                && colony.ControllingEmpireId == empireId
            select new EmpireControlledRacePopulationProjection(
                demographic.RaceId,
                !string.IsNullOrWhiteSpace(race.Name) ? race.Name : demographic.RaceName,
                raceHomeWorld != null ? raceHomeWorld.Name : null,
                demographic.Population,
                colony.FoundingEmpireId == empireId,
                colony.FoundingEmpireId != empireId))
            .ToListAsync(cancellationToken);

        var inferredControlledPopulationRows = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            join race in dbContext.AlienRaces.AsNoTracking() on (int)colony.RaceId equals race.Id into races
            from race in races.DefaultIfEmpty()
            join raceHomeWorld in dbContext.Worlds.AsNoTracking() on race.HomePlanetId equals raceHomeWorld.Id into raceHomeWorlds
            from raceHomeWorld in raceHomeWorlds.DefaultIfEmpty()
            where system.SectorId == sectorId
                && colony.ControllingEmpireId == empireId
                && !dbContext.Set<ColonyDemographic>().AsNoTracking().Any(demographic => demographic.ColonyId == colony.Id)
            select new EmpireControlledRacePopulationProjection(
                (int)colony.RaceId,
                !string.IsNullOrWhiteSpace(race.Name) ? race.Name : colony.ColonistRaceName,
                raceHomeWorld != null ? raceHomeWorld.Name : null,
                colony.EstimatedPopulation,
                colony.FoundingEmpireId == empireId,
                colony.FoundingEmpireId != empireId))
            .ToListAsync(cancellationToken);

        var memberRaces = BuildEmpireRaceMembershipDetails(
                empire,
                memberRaceRows,
                controlledDemographicRows.Concat(inferredControlledPopulationRows))
            .ToList();

        var colonies = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            join controllingEmpire in dbContext.Empires.AsNoTracking() on colony.ControllingEmpireId equals controllingEmpire.Id into controllingEmpires
            from controllingEmpire in controllingEmpires.DefaultIfEmpty()
            join controllingRace in dbContext.AlienRaces.AsNoTracking() on controllingEmpire.Founding.FoundingRaceId equals controllingRace.Id into controllingRaces
            from controllingRace in controllingRaces.DefaultIfEmpty()
            join controllingWorld in dbContext.Worlds.AsNoTracking() on controllingEmpire.Founding.FoundingWorldId equals controllingWorld.Id into controllingWorlds
            from controllingWorld in controllingWorlds.DefaultIfEmpty()
            where system.SectorId == sectorId
                && (colony.ControllingEmpireId == empireId || colony.FoundingEmpireId == empireId)
            orderby colony.EstimatedPopulation descending, world.Name, colony.Id
            select new EmpireColonyListingProjection(
                colony.Id,
                string.IsNullOrWhiteSpace(colony.Name) ? colony.ColonyClass : colony.Name,
                colony.EstimatedPopulation,
                colony.ControllingEmpireId == empireId,
                colony.ControllingEmpireId,
                controllingEmpire != null ? controllingEmpire.Id : (int?)null,
                controllingEmpire != null ? controllingEmpire.Name : null,
                controllingEmpire != null ? controllingEmpire.GovernmentType : null,
                controllingEmpire != null ? controllingEmpire.Founding.Origin : (EmpireOrigin?)null,
                controllingEmpire != null ? controllingEmpire.Founding.FoundingRaceId : null,
                controllingRace != null ? controllingRace.Name : null,
                controllingWorld != null ? controllingWorld.Name : null,
                world.Id,
                world.Name,
                system.Id,
                system.Name))
            .ToListAsync(cancellationToken);

        var relationships = await (
            from contact in dbContext.Set<EmpireContact>().AsNoTracking()
            join relatedEmpire in dbContext.Empires.AsNoTracking() on contact.OtherEmpireId equals relatedEmpire.Id
            join relatedRace in dbContext.AlienRaces.AsNoTracking() on relatedEmpire.Founding.FoundingRaceId equals relatedRace.Id into relatedRaces
            from relatedRace in relatedRaces.DefaultIfEmpty()
            join relatedWorld in dbContext.Worlds.AsNoTracking() on relatedEmpire.Founding.FoundingWorldId equals relatedWorld.Id into relatedWorlds
            from relatedWorld in relatedWorlds.DefaultIfEmpty()
            where contact.EmpireId == empireId
                && sectorEmpireIds.Contains(relatedEmpire.Id)
            orderby contact.RelationCode, relatedEmpire.Name, relatedEmpire.Id
            select new EmpireRelationshipProjection(
                relatedEmpire.Id,
                relatedEmpire.Name,
                relatedEmpire.GovernmentType,
                relatedEmpire.Founding.Origin,
                relatedEmpire.Founding.FoundingRaceId,
                relatedRace != null ? relatedRace.Name : null,
                relatedWorld != null ? relatedWorld.Name : null,
                contact.Relation,
                contact.Age))
            .ToListAsync(cancellationToken);

        var resolvedColonies = colonies
            .Select(colony => new ExplorerEmpireColonyListing(
                colony.ColonyId,
                colony.ColonyName,
                colony.EstimatedPopulation,
                colony.IsControlled,
                colony.ControllingEmpireId,
                colony.ControllingEmpireRecordId.HasValue
                    ? ResolveEmpireDisplayName(
                        colony.ControllingEmpireRecordId.Value,
                        colony.ControllingEmpireName,
                        colony.ControllingEmpireGovernmentType,
                        colony.ControllingEmpireOrigin ?? EmpireOrigin.Unknown,
                        colony.ControllingEmpireFoundingRaceId,
                        colony.ControllingEmpireFoundingRaceName,
                        colony.ControllingEmpireFoundingWorldName)
                    : null,
                colony.WorldId,
                colony.WorldName,
                colony.SystemId,
                colony.SystemName))
            .ToList();

        var resolvedRelationships = relationships
            .Select(contact => new ExplorerEmpireRelationshipListing(
                contact.OtherEmpireId,
                ResolveEmpireDisplayName(
                    contact.OtherEmpireId,
                    contact.OtherEmpireName,
                    contact.OtherEmpireGovernmentType,
                    contact.OtherEmpireOrigin,
                    contact.OtherEmpireFoundingRaceId,
                    contact.OtherEmpireFoundingRaceName,
                    contact.OtherEmpireFoundingWorldName),
                contact.Relation,
                contact.Age))
            .ToList();

        var controlledColonyCount = resolvedColonies.Count(colony => colony.IsControlled);

        var foundingRaceDisplayName = memberRaces
            .FirstOrDefault(item => item.RaceId == empire.Founding.FoundingRaceId)
            ?.RaceName;
        empire.Name = ResolveEmpireDisplayName(
            empire.Id,
            empire.Name,
            empire.GovernmentType,
            empire.Founding.Origin,
            empire.Founding.FoundingRaceId,
            foundingRaceDisplayName,
            homeWorld?.Name);

        return new ExplorerEmpireDetail(
            sectorId,
            empire,
            homeWorld,
            memberRaces,
            resolvedColonies,
            resolvedRelationships,
            controlledColonyCount,
            empire.IsFallen,
            BuildEmpireCivilizationModifierDetail(empire));
    }

    public async Task<ExplorerReligionFilterOptions> LoadReligionFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).EmpireIds;
        if (sectorEmpireIds.Count == 0)
        {
            return new ExplorerReligionFilterOptions([]);
        }

        var types = await (
            from religion in dbContext.Religions.AsNoTracking()
            join empireReligion in dbContext.Set<EmpireReligion>().AsNoTracking() on religion.Id equals empireReligion.ReligionId
            where sectorEmpireIds.Contains(empireReligion.EmpireId)
                && !string.IsNullOrWhiteSpace(religion.Type)
            select religion.Type)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync(cancellationToken);

        return new ExplorerReligionFilterOptions(types);
    }

    public async Task<ExplorerReligionListPage> LoadReligionListPageAsync(ExplorerReligionListPageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, request.SectorId, cancellationToken)).EmpireIds;
        var religionIds = await LoadSectorReligionIdsAsync(dbContext, sectorEmpireIds, cancellationToken);
        if (religionIds.Count == 0)
        {
            return new ExplorerReligionListPage([], false);
        }

        var religionsQuery = dbContext.Religions
            .AsNoTracking()
            .Where(religion => religionIds.Contains(religion.Id));

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchPattern = $"%{request.Query.Trim()}%";
            religionsQuery = religionsQuery.Where(religion =>
                EF.Functions.Like(religion.Name, searchPattern)
                || EF.Functions.Like(religion.Type, searchPattern));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var religionType = request.Type.Trim();
            religionsQuery = religionsQuery.Where(religion => religion.Type == religionType);
        }

        var items = await religionsQuery
            .OrderBy(religion => religion.Name)
            .ThenBy(religion => religion.Id)
            .Select(religion => new ExplorerReligionListItem(
                religion.Id,
                religion.Name,
                religion.Type,
                dbContext.Set<EmpireReligion>().Count(empireReligion =>
                    empireReligion.ReligionId == religion.Id
                    && sectorEmpireIds.Contains(empireReligion.EmpireId))))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > request.Limit;
        return new ExplorerReligionListPage(items.Take(request.Limit).ToList(), hasMore);
    }

    public async Task<ExplorerReligionListItem?> LoadReligionListItemAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).EmpireIds;
        var religionIds = await LoadSectorReligionIdsAsync(dbContext, sectorEmpireIds, cancellationToken);
        if (!religionIds.Contains(religionId))
        {
            return null;
        }

        return await dbContext.Religions
            .AsNoTracking()
            .Where(religion => religion.Id == religionId)
            .Select(religion => new ExplorerReligionListItem(
                religion.Id,
                religion.Name,
                religion.Type,
                dbContext.Set<EmpireReligion>().Count(empireReligion =>
                    empireReligion.ReligionId == religion.Id
                    && sectorEmpireIds.Contains(empireReligion.EmpireId))))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExplorerReligionDetail?> LoadReligionDetailAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sectorEmpireIds = (await GetCachedSectorEntityUsageAsync(dbContext, sectorId, cancellationToken)).EmpireIds;
        var religionIds = await LoadSectorReligionIdsAsync(dbContext, sectorEmpireIds, cancellationToken);
        if (!religionIds.Contains(religionId))
        {
            return null;
        }

        var religion = await dbContext.Religions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == religionId, cancellationToken);
        if (religion is null)
        {
            return null;
        }

        var empires = await (
            from empireReligion in dbContext.Set<EmpireReligion>().AsNoTracking()
            join empire in dbContext.Empires.AsNoTracking() on empireReligion.EmpireId equals empire.Id
            where empireReligion.ReligionId == religionId
                && sectorEmpireIds.Contains(empire.Id)
            orderby empireReligion.PopulationPercent descending, empire.Name, empire.Id
            select new ExplorerReligionEmpireListing(
                empire.Id,
                empire.Name,
                empireReligion.PopulationPercent,
                empire.NativePopulationMillions
                    + empire.CaptivePopulationMillions
                    + empire.SubjectPopulationMillions
                    + empire.IndependentPopulationMillions,
                empire.CivilizationProfile.TechLevel))
            .ToListAsync(cancellationToken);

        var races = await (
            from empireReligion in dbContext.Set<EmpireReligion>().AsNoTracking()
            join membership in dbContext.Set<EmpireRaceMembership>().AsNoTracking() on empireReligion.EmpireId equals membership.EmpireId
            join race in dbContext.AlienRaces.AsNoTracking() on membership.RaceId equals race.Id
            where empireReligion.ReligionId == religionId
                && sectorEmpireIds.Contains(empireReligion.EmpireId)
            group membership by new { race.Id, race.Name } into grouped
            orderby grouped.Sum(item => item.PopulationMillions) descending, grouped.Key.Name, grouped.Key.Id
            select new ExplorerReligionRaceListing(
                grouped.Key.Id,
                grouped.Key.Name,
                grouped.Select(item => item.EmpireId).Distinct().Count(),
                grouped.Sum(item => item.PopulationMillions)))
            .ToListAsync(cancellationToken);

        return new ExplorerReligionDetail(sectorId, religion, empires, races);
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

    private async Task<StarSystem?> LoadProjectedSystemDetailAsync(
        int sectorId,
        int systemId,
        bool includeWorldCharacteristics,
        bool includeColonies,
        bool includeColonyDemographics,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<StarSystem> query = dbContext.StarSystems
            .AsNoTracking()
            .AsSplitQuery()
            .Where(system => system.SectorId == sectorId && system.Id == systemId)
            .Include(system => system.AstralBodies)
            .Include(system => system.Worlds)
            .Include(system => system.SpaceHabitats);

        if (includeWorldCharacteristics)
        {
            query = query
                .Include(system => system.Worlds)
                    .ThenInclude(world => world.UnusualCharacteristics);
        }

        if (includeColonies)
        {
            query = query
                .Include(system => system.Worlds)
                    .ThenInclude(world => world.Colony);
        }

        if (includeColonyDemographics)
        {
            query = query
                .Include(system => system.Worlds)
                    .ThenInclude(world => world.Colony)
                        .ThenInclude(colony => colony!.Demographics);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static int RankSearchResult(StarWinSearchResult result, string query)
    {
        if (string.Equals(result.Title, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (result.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return result.Type switch
        {
            StarWinSearchResultType.StarSystem => 2,
            StarWinSearchResultType.World => 3,
            StarWinSearchResultType.Colony => 4,
            StarWinSearchResultType.SpaceHabitat => 5,
            StarWinSearchResultType.AlienRace => 6,
            StarWinSearchResultType.Empire => 7,
            StarWinSearchResultType.History => 8,
            _ => 9
        };
    }

    private static string DisplayCoordinates(Coordinates coordinates)
    {
        return $"{coordinates.XParsecs:0.#}, {coordinates.YParsecs:0.#}, {coordinates.ZParsecs:0.#}";
    }

    private static string DisplayPopulation(long population)
    {
        return population >= 1_000_000_000
            ? $"{population / 1_000_000_000m:0.#} billion"
            : $"{population / 1_000_000m:0.#} million";
    }

    private static SectorConfiguration CloneSectorConfiguration(SectorConfiguration configuration)
    {
        return new SectorConfiguration
        {
            SectorId = configuration.SectorId,
            OffLaneMaximumDistanceParsecs = configuration.OffLaneMaximumDistanceParsecs,
            Tl9AndBelowMaximumConnectionsPerSystem = configuration.Tl9AndBelowMaximumConnectionsPerSystem,
            AdditionalCrossEmpireConnectionsPerSystem = configuration.AdditionalCrossEmpireConnectionsPerSystem,
            Tl6HyperlaneName = configuration.Tl6HyperlaneName,
            Tl6MaximumDistanceParsecs = configuration.Tl6MaximumDistanceParsecs,
            Tl6OffLaneSpeedMultiplier = configuration.Tl6OffLaneSpeedMultiplier,
            Tl6HyperlaneSpeedModifier = configuration.Tl6HyperlaneSpeedModifier,
            Tl7HyperlaneName = configuration.Tl7HyperlaneName,
            Tl7MaximumDistanceParsecs = configuration.Tl7MaximumDistanceParsecs,
            Tl7OffLaneSpeedMultiplier = configuration.Tl7OffLaneSpeedMultiplier,
            Tl7HyperlaneSpeedModifier = configuration.Tl7HyperlaneSpeedModifier,
            Tl8HyperlaneName = configuration.Tl8HyperlaneName,
            Tl8MaximumDistanceParsecs = configuration.Tl8MaximumDistanceParsecs,
            Tl8OffLaneSpeedMultiplier = configuration.Tl8OffLaneSpeedMultiplier,
            Tl8HyperlaneSpeedModifier = configuration.Tl8HyperlaneSpeedModifier,
            Tl9HyperlaneName = configuration.Tl9HyperlaneName,
            Tl9MaximumDistanceParsecs = configuration.Tl9MaximumDistanceParsecs,
            Tl9OffLaneSpeedMultiplier = configuration.Tl9OffLaneSpeedMultiplier,
            Tl9HyperlaneSpeedModifier = configuration.Tl9HyperlaneSpeedModifier,
            Tl10HyperlaneName = configuration.Tl10HyperlaneName,
            Tl10MaximumDistanceParsecs = configuration.Tl10MaximumDistanceParsecs,
            Tl10OffLaneSpeedMultiplier = configuration.Tl10OffLaneSpeedMultiplier,
            Tl10HyperlaneSpeedModifier = configuration.Tl10HyperlaneSpeedModifier,
            UpdatedAtUtc = configuration.UpdatedAtUtc
        };
    }

    private static Colony CopyColony(Colony colony, bool includeDemographics)
    {
        var copy = new Colony
        {
            Id = colony.Id,
            WorldId = colony.WorldId,
            Name = colony.Name,
            WorldKind = colony.WorldKind,
            RaceId = colony.RaceId,
            ColonistRaceName = colony.ColonistRaceName,
            AllegianceId = colony.AllegianceId,
            AllegianceName = colony.AllegianceName,
            PoliticalStatus = colony.PoliticalStatus,
            ControllingEmpireId = colony.ControllingEmpireId,
            ParentEmpireId = colony.ParentEmpireId,
            FoundingEmpireId = colony.FoundingEmpireId,
            EncodedPopulation = colony.EncodedPopulation,
            EstimatedPopulation = colony.EstimatedPopulation,
            NativePopulationPercent = colony.NativePopulationPercent,
            ColonyClass = colony.ColonyClass,
            ColonyClassCode = colony.ColonyClassCode,
            Crime = colony.Crime,
            Law = colony.Law,
            Stability = colony.Stability,
            AgeCenturies = colony.AgeCenturies,
            Starport = colony.Starport,
            StarportCode = colony.StarportCode,
            GovernmentType = colony.GovernmentType,
            GovernmentTypeCode = colony.GovernmentTypeCode,
            GrossWorldProductMcr = colony.GrossWorldProductMcr,
            MilitaryPower = colony.MilitaryPower,
            ExportResource = colony.ExportResource,
            ExportResourceCode = colony.ExportResourceCode,
            ImportResource = colony.ImportResource,
            ImportResourceCode = colony.ImportResourceCode
        };

        foreach (var facility in colony.Facilities)
        {
            copy.Facilities.Add(facility);
        }

        if (includeDemographics)
        {
            foreach (var demographic in colony.Demographics)
            {
                copy.Demographics.Add(new ColonyDemographic
                {
                    ColonyId = demographic.ColonyId,
                    RaceId = demographic.RaceId,
                    RaceName = demographic.RaceName,
                    PopulationPercent = demographic.PopulationPercent,
                    Population = demographic.Population
                });
            }
        }

        return copy;
    }

    private static World CopyWorld(World world, bool includeColony, bool includeCharacteristics)
    {
        var copy = new World
        {
            Id = world.Id,
            Kind = world.Kind,
            LegacyPlanetId = world.LegacyPlanetId,
            LegacyMoonId = world.LegacyMoonId,
            StarSystemId = world.StarSystemId,
            ParentWorldId = world.ParentWorldId,
            PrimaryAstralBodySequence = world.PrimaryAstralBodySequence,
            Name = world.Name,
            WorldType = world.WorldType,
            AtmosphereType = world.AtmosphereType,
            AtmosphereComposition = world.AtmosphereComposition,
            WaterType = world.WaterType,
            Hydrography = new Hydrography
            {
                WaterPercent = world.Hydrography.WaterPercent,
                IcePercent = world.Hydrography.IcePercent,
                CloudPercent = world.Hydrography.CloudPercent
            },
            MineralResources = new MineralResources
            {
                MetalOre = world.MineralResources.MetalOre,
                RadioactiveOre = world.MineralResources.RadioactiveOre,
                PreciousMetal = world.MineralResources.PreciousMetal,
                RawCrystals = world.MineralResources.RawCrystals,
                PreciousGems = world.MineralResources.PreciousGems
            },
            DiameterKm = world.DiameterKm,
            DensityTenthsEarth = world.DensityTenthsEarth,
            AtmosphericPressure = world.AtmosphericPressure,
            AverageTemperatureCelsius = world.AverageTemperatureCelsius,
            MiscellaneousFlags = world.MiscellaneousFlags,
            Albedo = world.Albedo,
            AlienRaceId = world.AlienRaceId,
            ControlledByEmpireId = world.ControlledByEmpireId,
            AllegianceId = world.AllegianceId,
            OrbitRadiusAu = world.OrbitRadiusAu,
            OrbitRadiusKm = world.OrbitRadiusKm,
            SmallestMolecularWeightRetained = world.SmallestMolecularWeightRetained,
            AxialTiltDegrees = world.AxialTiltDegrees,
            OrbitalInclinationDegrees = world.OrbitalInclinationDegrees,
            RotationPeriodHours = world.RotationPeriodHours,
            EccentricityThousandths = world.EccentricityThousandths,
            OrbitPeriodDays = world.OrbitPeriodDays,
            GravityEarthG = world.GravityEarthG,
            MassEarthMasses = world.MassEarthMasses,
            EscapeVelocityKmPerSecond = world.EscapeVelocityKmPerSecond,
            OxygenPressureAtmospheres = world.OxygenPressureAtmospheres,
            BoilingPointCelsius = world.BoilingPointCelsius,
            MagneticFieldGauss = world.MagneticFieldGauss
        };

        if (includeCharacteristics)
        {
            foreach (var characteristic in world.UnusualCharacteristics)
            {
                copy.UnusualCharacteristics.Add(new UnusualCharacteristic
                {
                    Code = characteristic.Code,
                    Name = characteristic.Name,
                    Notes = characteristic.Notes
                });
            }
        }

        if (includeColony && world.Colony is not null)
        {
            copy.Colony = CopyColony(world.Colony, includeDemographics: true);
        }

        return copy;
    }

    private static IQueryable<Empire> ApplyEmpireFilters(
        StarWinDbContext dbContext,
        IQueryable<Empire> empiresQuery,
        ExplorerEmpireListPageRequest request)
    {
        if (request.RaceId is int raceId)
        {
            empiresQuery = empiresQuery.Where(empire => empire.RaceMemberships.Any(membership => membership.RaceId == raceId));
        }

        if (request.FallenOnly)
        {
            empiresQuery = empiresQuery.Where(empire => empire.IsFallen);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchPattern = $"%{request.Query.Trim()}%";
            empiresQuery = empiresQuery.Where(empire =>
                EF.Functions.Like(empire.Name, searchPattern)
                || dbContext.EntityNotes.Any(note =>
                    note.TargetId == empire.Id
                    && (note.TargetKind == EntityNoteTargetKind.Empire
                        || note.TargetKind == EntityNoteTargetKind.EmpireSummary)
                    && EF.Functions.Like(note.Markdown, searchPattern)));
        }

        return empiresQuery;
    }

    private static IQueryable<AlienRace> ApplyAlienRaceFilters(
        StarWinDbContext dbContext,
        IQueryable<AlienRace> racesQuery,
        HashSet<int> sectorEmpireIds,
        ExplorerAlienRaceListPageRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchPattern = $"%{request.Query.Trim()}%";
            racesQuery = racesQuery.Where(race =>
                EF.Functions.Like(race.Name, searchPattern)
                || dbContext.EntityNotes.Any(note =>
                    note.TargetKind == EntityNoteTargetKind.AlienRace
                    && note.TargetId == race.Id
                    && EF.Functions.Like(note.Markdown, searchPattern)));
        }

        if (!string.IsNullOrWhiteSpace(request.EnvironmentType))
        {
            var environmentType = request.EnvironmentType.Trim();
            racesQuery = racesQuery.Where(race => race.EnvironmentType == environmentType);
        }

        if (!string.IsNullOrWhiteSpace(request.AppearanceType))
        {
            var appearanceType = request.AppearanceType.Trim();
            racesQuery = racesQuery.Where(race => race.AppearanceType == appearanceType);
        }

        if (request.MaxTotalPointCost is int maxTotalPointCost)
        {
            racesQuery = racesQuery.Where(race =>
                (((race.BiologyProfile.Body >= 16 ? 3 : race.BiologyProfile.Body <= 4 ? -3 : ((int)race.BiologyProfile.Body - 10) / 2) * 10)
                + ((race.BiologyProfile.Mind >= 16 ? 3 : race.BiologyProfile.Mind <= 4 ? -3 : ((int)race.BiologyProfile.Mind - 10) / 2) * 20)
                + ((race.BiologyProfile.Speed >= 16 ? 3 : race.BiologyProfile.Speed <= 4 ? -3 : ((int)race.BiologyProfile.Speed - 10) / 2) * 20)
                + (race.MassKg >= 130 ? 10 : race.MassKg <= 50 ? -10 : 0)
                + (race.BiologyProfile.Speed >= 12 ? 5 : 0)
                + (race.BiologyProfile.Lifespan >= 36 ? 2 : 0)
                + (race.BiologyProfile.PsiRating >= PsiPowerRating.Good ? Math.Max(1, (int)race.BiologyProfile.PsiPower) * 5 : 0)
                + (race.EnvironmentType.Contains("Vacuum") ? 5 : 0)
                + (race.EnvironmentType.Contains("Aquatic") ? 10 : 0)
                + (race.AppearanceType.Contains("Avian") ? 40 : 0)
                + (race.BodyCoverType.Contains("Hard") || race.BodyCoverType.Contains("Crystal") || race.BodyCoverType.Contains("Scales") ? 5 : 0)
                + (sectorEmpireIds.Count > 0 && dbContext.Empires.Any(empire =>
                    sectorEmpireIds.Contains(empire.Id)
                    && empire.CivilizationProfile.TechLevel >= 9
                    && empire.RaceMemberships.Any(membership => membership.RaceId == race.Id)) ? 1 : 0)
                + (race.EnvironmentType.Contains("Subterranean") ? -10 : 0)
                + (race.Diet.Contains("Mineral") || race.Diet.Contains("Energy") ? -10 : 0)
                + ((race.BiologyProfile.PsiRating == PsiPowerRating.VeryPoor || race.BiologyProfile.PsiRating == PsiPowerRating.Poor) ? -5 : 0)
                + (race.EnvironmentType.Contains("Aquatic") ? 1 : 0)
                + (race.EnvironmentType.Contains("Aerial") ? 2 : 0)
                + (race.BiologyProfile.Mind >= 12 ? 2 : 0)) <= maxTotalPointCost);
        }

        if (request.StarWinTechLevel is byte starWinTechLevel)
        {
            racesQuery = racesQuery.Where(race => dbContext.Empires.Any(empire =>
                sectorEmpireIds.Contains(empire.Id)
                && empire.CivilizationProfile.TechLevel == starWinTechLevel
                && empire.RaceMemberships.Any(membership => membership.RaceId == race.Id)));
        }

        var requireSuperscience = request.RequireSuperscience;
        if (GurpsTechnologyLevelMapper.TryParseDisplay(request.GurpsTechLevel, out var gurpsTechLevel, out var gurpsSuperscience))
        {
            requireSuperscience |= gurpsSuperscience;
            var mappedStarWinTechLevel = gurpsTechLevel - 2;
            racesQuery = racesQuery.Where(race => dbContext.Empires.Any(empire =>
                sectorEmpireIds.Contains(empire.Id)
                && empire.CivilizationProfile.TechLevel == mappedStarWinTechLevel
                && empire.RaceMemberships.Any(membership => membership.RaceId == race.Id)));
        }

        if (requireSuperscience)
        {
            racesQuery = racesQuery.Where(race =>
                (from colony in dbContext.Colonies
                 join colonyWorld in dbContext.Worlds on colony.WorldId equals colonyWorld.Id
                 join homeWorld in dbContext.Worlds on race.HomePlanetId equals homeWorld.Id
                 where colony.RaceId == race.Id
                    && colonyWorld.StarSystemId != homeWorld.StarSystemId
                 select colony.Id)
                .Any());
        }

        return racesQuery;
    }

    private async Task<HashSet<int>> LoadSectorRaceIdsAsync(StarWinDbContext dbContext, int sectorId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Explorer query start {Operation}. sectorId={SectorId}", nameof(LoadSectorRaceIdsAsync), sectorId);
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

        logger.LogInformation(
            "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceCount={RaceCount}",
            nameof(LoadSectorRaceIdsAsync),
            stopwatch.ElapsedMilliseconds,
            sectorId,
            raceIds.Count);

        return raceIds;
    }

    private static async Task<HashSet<int>> LoadSuperscienceRaceIdsAsync(StarWinDbContext dbContext, HashSet<int> raceIds, CancellationToken cancellationToken)
    {
        if (raceIds.Count == 0)
        {
            return [];
        }

        var superscienceRaceIds = await (
            from race in dbContext.AlienRaces.AsNoTracking()
            join homeWorld in dbContext.Worlds.AsNoTracking() on race.HomePlanetId equals homeWorld.Id
            join colony in dbContext.Colonies.AsNoTracking() on race.Id equals colony.RaceId
            join colonyWorld in dbContext.Worlds.AsNoTracking() on colony.WorldId equals colonyWorld.Id
            where raceIds.Contains(race.Id)
                && colonyWorld.StarSystemId != homeWorld.StarSystemId
            select race.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        return superscienceRaceIds.ToHashSet();
    }

    private static async Task<HashSet<int>> LoadSectorReligionIdsAsync(StarWinDbContext dbContext, HashSet<int> sectorEmpireIds, CancellationToken cancellationToken)
    {
        if (sectorEmpireIds.Count == 0)
        {
            return [];
        }

        var religionIds = await dbContext.Set<EmpireReligion>()
            .AsNoTracking()
            .Where(religion => sectorEmpireIds.Contains(religion.EmpireId))
            .Select(religion => religion.ReligionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return religionIds.ToHashSet();
    }

    private async Task<HashSet<int>> LoadSectorEmpireIdsAsync(StarWinDbContext dbContext, int sectorId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Explorer query start {Operation}. sectorId={SectorId}", nameof(LoadSectorEmpireIdsAsync), sectorId);
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

        logger.LogInformation(
            "Explorer query complete {Operation} in {ElapsedMs}ms. sectorId={SectorId} empireCount={EmpireCount}",
            nameof(LoadSectorEmpireIdsAsync),
            stopwatch.ElapsedMilliseconds,
            sectorId,
            empireIds.Count);

        return empireIds;
    }

    private async Task<CachedSectorEntityUsage> GetCachedSectorEntityUsageAsync(
        StarWinDbContext dbContext,
        int sectorId,
        CancellationToken cancellationToken)
    {
        if (sectorId <= 0)
        {
            return EmptyCachedSectorEntityUsage;
        }

        if (sectorEntityUsageCache.TryGetValue(sectorId, out var cachedUsage)
            && cachedUsage.ExpiresAtUtc > DateTime.UtcNow)
        {
            logger.LogInformation(
                "Explorer query cache hit {Operation}. sectorId={SectorId} raceCount={RaceCount} empireCount={EmpireCount}",
                nameof(GetCachedSectorEntityUsageAsync),
                sectorId,
                cachedUsage.RaceIds.Count,
                cachedUsage.EmpireIds.Count);
            return cachedUsage;
        }

        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Explorer query cache miss {Operation}. sectorId={SectorId}", nameof(GetCachedSectorEntityUsageAsync), sectorId);
        var raceIds = await LoadSectorRaceIdsAsync(dbContext, sectorId, cancellationToken);
        var empireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
        var cachedValue = new CachedSectorEntityUsage(
            DateTime.UtcNow.Add(SectorEntityUsageCacheDuration),
            new ExplorerSectorEntityUsage(
                sectorId,
                raceIds.OrderBy(id => id).ToList(),
                empireIds.OrderBy(id => id).ToList()),
            raceIds,
            empireIds);

        sectorEntityUsageCache[sectorId] = cachedValue;
        logger.LogInformation(
            "Explorer query cache populated {Operation} in {ElapsedMs}ms. sectorId={SectorId} raceCount={RaceCount} empireCount={EmpireCount}",
            nameof(GetCachedSectorEntityUsageAsync),
            stopwatch.ElapsedMilliseconds,
            sectorId,
            raceIds.Count,
            empireIds.Count);
        return cachedValue;
    }

    private static async Task<Dictionary<int, EmpireWorldCountProjection>> LoadEmpireWorldCountsByEmpireIdAsync(
        StarWinDbContext dbContext,
        int sectorId,
        IReadOnlyCollection<int> empireIds,
        CancellationToken cancellationToken)
    {
        if (sectorId <= 0 || empireIds.Count == 0)
        {
            return [];
        }

        var empireIdSet = empireIds.Distinct().ToHashSet();
        var controlledWorldLinks = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && colony.ControllingEmpireId.HasValue
                && empireIdSet.Contains(colony.ControllingEmpireId.Value)
            select new EmpireWorldLinkProjection(colony.ControllingEmpireId!.Value, colony.WorldId))
            .Distinct()
            .ToListAsync(cancellationToken);

        var foundedWorldLinks = await (
            from colony in dbContext.Colonies.AsNoTracking()
            join world in dbContext.Worlds.AsNoTracking() on colony.WorldId equals world.Id
            join system in dbContext.StarSystems.AsNoTracking() on world.StarSystemId equals system.Id
            where system.SectorId == sectorId
                && colony.FoundingEmpireId.HasValue
                && empireIdSet.Contains(colony.FoundingEmpireId.Value)
            select new EmpireWorldLinkProjection(colony.FoundingEmpireId!.Value, colony.WorldId))
            .Distinct()
            .ToListAsync(cancellationToken);

        var trackedWorldLinks = controlledWorldLinks
            .Concat(foundedWorldLinks)
            .Distinct()
            .ToList();

        var controlledCountsByEmpireId = controlledWorldLinks
            .GroupBy(link => link.EmpireId)
            .ToDictionary(group => group.Key, group => group.Count());
        var trackedCountsByEmpireId = trackedWorldLinks
            .GroupBy(link => link.EmpireId)
            .ToDictionary(group => group.Key, group => group.Count());

        return empireIdSet.ToDictionary(
            empireId => empireId,
            empireId => new EmpireWorldCountProjection(
                controlledCountsByEmpireId.GetValueOrDefault(empireId),
                trackedCountsByEmpireId.GetValueOrDefault(empireId)));
    }

    private static string ResolveRaceDisplayName(int raceId, string? raceName, string? homeWorldName)
    {
        if (!IsPlaceholderRaceName(raceName))
        {
            return string.IsNullOrWhiteSpace(raceName)
                ? $"Race {raceId}"
                : raceName.Trim();
        }

        var normalizedRoot = NormalizeNameRoot(homeWorldName);
        return string.IsNullOrWhiteSpace(normalizedRoot)
            ? $"Race {raceId}"
            : BuildSpeciesNameFromRoot(normalizedRoot);
    }

    private static string ResolveEmpireDisplayName(
        int empireId,
        string? empireName,
        string? governmentType,
        EmpireOrigin origin,
        int? foundingRaceId,
        string? foundingRaceName,
        string? foundingWorldName)
    {
        var resolvedRaceName = foundingRaceId is int raceId
            ? ResolveRaceDisplayName(raceId, foundingRaceName, foundingWorldName)
            : string.Empty;

        if (origin == EmpireOrigin.IndependentColony
            && !string.IsNullOrWhiteSpace(resolvedRaceName)
            && (IsPlaceholderEmpireName(empireName)
                || empireName?.EndsWith(" Independent State", StringComparison.OrdinalIgnoreCase) == true))
        {
            return $"{resolvedRaceName} Independent State";
        }

        if (!IsPlaceholderEmpireName(empireName))
        {
            return empireName!.Trim();
        }

        if (!string.IsNullOrWhiteSpace(resolvedRaceName))
        {
            return BuildEmpireNameFromGovernment(resolvedRaceName, governmentType);
        }

        var normalizedRoot = NormalizeNameRoot(foundingWorldName);
        if (!string.IsNullOrWhiteSpace(normalizedRoot))
        {
            return BuildEmpireNameFromGovernment(BuildSpeciesNameFromRoot(normalizedRoot), governmentType);
        }

        return string.IsNullOrWhiteSpace(empireName)
            ? $"Empire {empireId}"
            : empireName.Trim();
    }

    private static ExplorerEmpireListItem BuildEmpireListItem(EmpireListProjection item)
    {
        return new ExplorerEmpireListItem(
            item.EmpireId,
            ResolveEmpireDisplayName(
                item.EmpireId,
                item.Name,
                item.GovernmentType,
                item.Origin,
                item.FoundingRaceId,
                item.FoundingRaceName,
                item.FoundingWorldName),
            item.ControlledWorldCount,
            item.TrackedWorldCount,
            item.GurpsTechLevel,
            item.IsFallen);
    }

    private static bool IsPlaceholderRaceName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            || name.Trim().StartsWith("Race ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlaceholderEmpireName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            || name.Trim().StartsWith("Empire ", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeNameRoot(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            var suffix = parts[^1];
            if (int.TryParse(suffix, out _)
                || IsRomanNumeral(suffix)
                || (suffix.Length == 1 && char.IsLetter(suffix[0])))
            {
                trimmed = string.Join(' ', parts[..^1]);
            }
        }

        return trimmed.Trim();
    }

    private static bool IsRomanNumeral(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.All(character => "IVXLCDMivxlcdm".Contains(character));
    }

    private static string BuildSpeciesNameFromRoot(string root)
    {
        if (root.EndsWith("a", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root[..^1]}an";
        }

        if (root.EndsWith("earth", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root}ling";
        }

        if (root.EndsWith("sol", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root}an";
        }

        if (root.EndsWith("erra", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root[..^1]}n";
        }

        if (root.EndsWith("el", StringComparison.OrdinalIgnoreCase)
            || root.EndsWith("il", StringComparison.OrdinalIgnoreCase)
            || root.EndsWith("ol", StringComparison.OrdinalIgnoreCase))
        {
            return $"{root}ian";
        }

        return $"{root}ian";
    }

    private static string BuildEmpireNameFromGovernment(string speciesName, string? governmentType)
    {
        var template = governmentType switch
        {
            "Imperialism" => "{0} Empire",
            "Democracy" or "Republic" or "Federation" => "{0} Republic",
            "Theocracy" => "{0} Church",
            "Corporation" => "{0} Combine",
            "Monarchy" => "Kingdom of {0}",
            _ => "{0} State"
        };

        return string.Format(template, speciesName).Trim();
    }

    private static readonly EmpireWorldCountProjection EmptyEmpireWorldCountProjection = new(0, 0);

    private static readonly CachedSectorEntityUsage EmptyCachedSectorEntityUsage = new(
        DateTime.MaxValue,
        new ExplorerSectorEntityUsage(0, [], []),
        [],
        []);

    private sealed record CachedSectorEntityUsage(
        DateTime ExpiresAtUtc,
        ExplorerSectorEntityUsage Value,
        HashSet<int> RaceIds,
        HashSet<int> EmpireIds);

    private static IReadOnlyList<ExplorerEmpireRaceMembershipDetail> BuildEmpireRaceMembershipDetails(
        Empire empire,
        IReadOnlyList<EmpireRaceMembershipProjection> membershipRows,
        IEnumerable<EmpireControlledRacePopulationProjection> controlledPopulationRows)
    {
        var membershipByRaceId = membershipRows.ToDictionary(row => row.RaceId);
        var controlledGroups = controlledPopulationRows
            .Where(row => row.RaceId > 0 && row.Population > 0)
            .GroupBy(row => row.RaceId)
            .Select(group => new EmpireControlledRaceAggregate(
                group.Key,
                group.Select(row => row.RaceName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"Race {group.Key}",
                group.Select(row => row.HomeWorldName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)),
                group.Sum(row => row.Population),
                group.Any(row => row.FoundedByEmpire),
                group.Any(row => row.ForeignFounded)))
            .ToList();

        if (controlledGroups.Count > 0)
        {
            var totalControlledPopulation = controlledGroups.Sum(group => group.Population);
            return controlledGroups
                .Select(group =>
                {
                    membershipByRaceId.TryGetValue(group.RaceId, out var membership);
                    return new ExplorerEmpireRaceMembershipDetail(
                        group.RaceId,
                        ResolveRaceDisplayName(group.RaceId, membership?.RaceName ?? group.RaceName, membership?.HomeWorldName ?? group.HomeWorldName),
                        membership?.Role ?? DeriveEmpireRaceRole(group),
                        ToPopulationMillions(group.Population),
                        CalculatePopulationPercent(group.Population, totalControlledPopulation),
                        membership?.IsPrimary ?? empire.Founding.FoundingRaceId == group.RaceId);
                })
                .OrderByDescending(item => item.PopulationMillions)
                .ThenByDescending(item => item.IsPrimary)
                .ThenBy(item => item.RaceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.RaceId)
                .ToList();
        }

        var totalMembershipPopulationMillions = membershipRows.Sum(row => row.PopulationMillions);
        return membershipRows
            .Select(member => new ExplorerEmpireRaceMembershipDetail(
                member.RaceId,
                ResolveRaceDisplayName(member.RaceId, member.RaceName, member.HomeWorldName),
                member.Role,
                member.PopulationMillions,
                CalculatePopulationPercent(member.PopulationMillions, totalMembershipPopulationMillions),
                member.IsPrimary))
            .OrderByDescending(item => item.PopulationMillions)
            .ThenByDescending(item => item.IsPrimary)
            .ThenBy(item => item.RaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.RaceId)
            .ToList();
    }

    private static ExplorerEmpireCivilizationModifierDetail BuildEmpireCivilizationModifierDetail(Empire empire)
    {
        return new ExplorerEmpireCivilizationModifierDetail(
            [
                BuildCivilizationTraitModifier("Militancy", empire.CivilizationModifiers.Militancy),
                BuildCivilizationTraitModifier("Determination", empire.CivilizationModifiers.Determination),
                BuildCivilizationTraitModifier("Racial tolerance", empire.CivilizationModifiers.RacialTolerance),
                BuildCivilizationTraitModifier("Progressiveness", empire.CivilizationModifiers.Progressiveness),
                BuildCivilizationTraitModifier("Loyalty", empire.CivilizationModifiers.Loyalty),
                BuildCivilizationTraitModifier("Social cohesion", empire.CivilizationModifiers.SocialCohesion),
                BuildCivilizationTraitModifier("Art", empire.CivilizationModifiers.Art),
                BuildCivilizationTraitModifier("Individualism", empire.CivilizationModifiers.Individualism)
            ]);
    }

    private static ExplorerCivilizationTraitModifier BuildCivilizationTraitModifier(
        string name,
        int modifier)
    {
        return new ExplorerCivilizationTraitModifier(name, modifier);
    }

    private static EmpireRaceRole DeriveEmpireRaceRole(EmpireControlledRaceAggregate aggregate)
    {
        if (aggregate.ForeignFounded)
        {
            return EmpireRaceRole.Subjugated;
        }

        if (aggregate.FoundedByEmpire)
        {
            return EmpireRaceRole.Member;
        }

        return EmpireRaceRole.Subject;
    }

    private static long ToPopulationMillions(long population)
    {
        if (population <= 0)
        {
            return 0;
        }

        return Math.Max(1L, (long)Math.Round(population / 1_000_000d, MidpointRounding.AwayFromZero));
    }

    private static decimal CalculatePopulationPercent(long population, long totalPopulation)
    {
        if (population <= 0 || totalPopulation <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)population * 100m / totalPopulation, 1, MidpointRounding.AwayFromZero);
    }

    private sealed class ExplorerSectorOverviewRow
    {
        public int SectorId { get; set; }

        public int SystemCount { get; set; }

        public int WorldCount { get; set; }

        public int ColonyCount { get; set; }

        public int EmpireCount { get; set; }

        public int RaceCount { get; set; }
    }

    private sealed record RaceDisplayProjection(int RaceId, string RaceName, string? HomeWorldName);

    private sealed record EmpireWorldLinkProjection(int EmpireId, int WorldId);

    private sealed record EmpireWorldCountProjection(int ControlledWorldCount, int TrackedWorldCount);

    private sealed record EmpireListHeaderProjection(
        int EmpireId,
        string Name,
        int GurpsTechLevel,
        string GovernmentType,
        EmpireOrigin Origin,
        int? FoundingRaceId,
        int? FoundingWorldId,
        string? FoundingRaceName,
        string? FoundingWorldName,
        bool IsFallen);

    private sealed record EmpireListProjection(
        int EmpireId,
        string Name,
        int ControlledWorldCount,
        int TrackedWorldCount,
        int GurpsTechLevel,
        string GovernmentType,
        EmpireOrigin Origin,
        int? FoundingRaceId,
        int? FoundingWorldId,
        string? FoundingRaceName,
        string? FoundingWorldName,
        bool IsFallen);

    private sealed record EmpireColonyListingProjection(
        int ColonyId,
        string ColonyName,
        long EstimatedPopulation,
        bool IsControlled,
        int? ControllingEmpireId,
        int? ControllingEmpireRecordId,
        string? ControllingEmpireName,
        string? ControllingEmpireGovernmentType,
        EmpireOrigin? ControllingEmpireOrigin,
        int? ControllingEmpireFoundingRaceId,
        string? ControllingEmpireFoundingRaceName,
        string? ControllingEmpireFoundingWorldName,
        int WorldId,
        string WorldName,
        int SystemId,
        string SystemName);

    private sealed record EmpireRelationshipProjection(
        int OtherEmpireId,
        string OtherEmpireName,
        string OtherEmpireGovernmentType,
        EmpireOrigin OtherEmpireOrigin,
        int? OtherEmpireFoundingRaceId,
        string? OtherEmpireFoundingRaceName,
        string? OtherEmpireFoundingWorldName,
        string Relation,
        byte Age);

    private sealed record EmpireRaceMembershipProjection(
        int RaceId,
        string RaceName,
        string? HomeWorldName,
        EmpireRaceRole Role,
        long PopulationMillions,
        bool IsPrimary);

    private sealed record EmpireControlledRacePopulationProjection(
        int RaceId,
        string RaceName,
        string? HomeWorldName,
        long Population,
        bool FoundedByEmpire,
        bool ForeignFounded);

    private sealed record EmpireControlledRaceAggregate(
        int RaceId,
        string RaceName,
        string? HomeWorldName,
        long Population,
        bool FoundedByEmpire,
        bool ForeignFounded);

    private sealed record AlienRaceEmpireDetailProjection(
        int EmpireId,
        string Name,
        string GovernmentType,
        byte Militancy,
        byte Determination,
        byte RacialTolerance,
        byte Progressiveness,
        byte Loyalty,
        byte SocialCohesion,
        byte TechLevel,
        byte Art,
        byte Individualism,
        byte SpatialAge,
        int MilitancyModifier,
        int DeterminationModifier,
        int RacialToleranceModifier,
        int ProgressivenessModifier,
        int LoyaltyModifier,
        int SocialCohesionModifier,
        int ArtModifier,
        int IndividualismModifier);
}
