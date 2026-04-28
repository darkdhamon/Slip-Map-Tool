using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Services;
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

    public async Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var raceIds = await LoadSectorRaceIdsAsync(dbContext, sectorId, cancellationToken);
        if (raceIds.Count == 0)
        {
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

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
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

        return new ExplorerAlienRaceFilterOptions(
            environmentTypes,
            appearanceTypes,
            starWinTechLevels,
            gurpsTechLevelPairs
                .Select(value => GurpsTechnologyLevelMapper.FormatDisplay(value.GurpsTechLevel, superscienceRaceIds.Contains(value.RaceId)))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList());
    }

    public async Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var raceIds = await LoadSectorRaceIdsAsync(dbContext, request.SectorId, cancellationToken);
        if (raceIds.Count == 0)
        {
            return new ExplorerAlienRaceListPage([], false);
        }

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, request.SectorId, cancellationToken);
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

    public async Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
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

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, request.SectorId, cancellationToken);
        if (sectorEmpireIds.Count == 0)
        {
            return new ExplorerEmpireListPage([], false);
        }

        var empiresQuery = dbContext.Empires
            .AsNoTracking()
            .Where(empire => sectorEmpireIds.Contains(empire.Id));

        empiresQuery = ApplyEmpireFilters(dbContext, empiresQuery, request);

        var items = await empiresQuery
            .OrderBy(empire => empire.Name)
            .ThenBy(empire => empire.Id)
            .Select(empire => new EmpireListProjection(
                empire.Id,
                empire.Name,
                dbContext.Colonies
                    .Where(colony =>
                        colony.ControllingEmpireId == empire.Id
                        && dbContext.Worlds
                            .Where(world => world.Id == colony.WorldId)
                            .Join(
                                dbContext.StarSystems,
                                world => world.StarSystemId,
                                system => system.Id,
                                (world, system) => system.SectorId)
                            .Contains(request.SectorId))
                    .Select(colony => colony.WorldId)
                    .Distinct()
                    .Count(),
                dbContext.Colonies
                    .Where(colony =>
                        (colony.ControllingEmpireId == empire.Id || colony.FoundingEmpireId == empire.Id)
                        && dbContext.Worlds
                            .Where(world => world.Id == colony.WorldId)
                            .Join(
                                dbContext.StarSystems,
                                world => world.StarSystemId,
                                system => system.Id,
                                (world, system) => system.SectorId)
                            .Contains(request.SectorId))
                    .Select(colony => colony.WorldId)
                    .Distinct()
                    .Count(),
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

        var hasMore = items.Count > request.Limit;
        return new ExplorerEmpireListPage(
            items
                .Take(request.Limit)
                .Select(BuildEmpireListItem)
                .ToList(),
            hasMore);
    }

    public async Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
        if (!sectorEmpireIds.Contains(empireId))
        {
            return null;
        }

        var item = await dbContext.Empires
            .AsNoTracking()
            .Where(empire => empire.Id == empireId)
            .Select(empire => new EmpireListProjection(
                empire.Id,
                empire.Name,
                dbContext.Colonies
                    .Where(colony =>
                        colony.ControllingEmpireId == empire.Id
                        && dbContext.Worlds
                            .Where(world => world.Id == colony.WorldId)
                            .Join(
                                dbContext.StarSystems,
                                world => world.StarSystemId,
                                system => system.Id,
                                (world, system) => system.SectorId)
                            .Contains(sectorId))
                    .Select(colony => colony.WorldId)
                    .Distinct()
                    .Count(),
                dbContext.Colonies
                    .Where(colony =>
                        (colony.ControllingEmpireId == empire.Id || colony.FoundingEmpireId == empire.Id)
                        && dbContext.Worlds
                            .Where(world => world.Id == colony.WorldId)
                            .Join(
                                dbContext.StarSystems,
                                world => world.StarSystemId,
                                system => system.Id,
                                (world, system) => system.SectorId)
                            .Contains(sectorId))
                    .Select(colony => colony.WorldId)
                    .Distinct()
                    .Count(),
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

        return item is null
            ? null
            : BuildEmpireListItem(item);
    }

    public async Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
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

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
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

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, request.SectorId, cancellationToken);
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

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
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

        var sectorEmpireIds = await LoadSectorEmpireIdsAsync(dbContext, sectorId, cancellationToken);
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

    private sealed record RaceDisplayProjection(int RaceId, string RaceName, string? HomeWorldName);

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
}
