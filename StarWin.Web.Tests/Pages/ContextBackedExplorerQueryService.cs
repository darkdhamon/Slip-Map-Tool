using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Web.Tests.Pages;

internal sealed class ContextBackedExplorerQueryService(StarWinExplorerContext context) : IStarWinExplorerQueryService
{
    public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null)
        {
            return Task.FromResult(new ExplorerSectorOverviewData(sectorId, 0, 0, 0, 0, 0));
        }

        var raceIds = GetRaceIds(sector);
        var empireIds = GetEmpireIds(sector);
        return Task.FromResult(new ExplorerSectorOverviewData(
            sectorId,
            sector.Systems.Count,
            sector.Systems.SelectMany(system => system.Worlds).Count(),
            sector.Systems.SelectMany(system => system.Worlds).Count(world => world.Colony is not null),
            empireIds.Count,
            raceIds.Count));
    }

    public Task<ExplorerSectorEntityUsage> LoadSectorEntityUsageAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null)
        {
            return Task.FromResult(new ExplorerSectorEntityUsage(sectorId, [], []));
        }

        return Task.FromResult(new ExplorerSectorEntityUsage(
            sectorId,
            GetRaceIds(sector).OrderBy(id => id).ToList(),
            GetEmpireIds(sector).OrderBy(id => id).ToList()));
    }

    public Task<IReadOnlyList<StarWinSearchResult>> SearchSectorAsync(int sectorId, string query, int maxResults = 30, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null || string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<StarWinSearchResult>>([]);
        }

        var normalizedQuery = query.Trim();
        var results = new List<StarWinSearchResult>();

        results.AddRange(sector.Systems
            .Where(system => Matches(normalizedQuery, system.Name, system.Id.ToString(), system.LegacySystemId?.ToString(), GetEmpireName(system.AllegianceId)))
            .OrderBy(system => system.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(system => system.Id)
            .Select(system => new StarWinSearchResult
            {
                Type = StarWinSearchResultType.StarSystem,
                Title = system.Name,
                Subtitle = $"System {system.Id}",
                Tab = "Systems",
                SectorId = sectorId,
                SystemId = system.Id
            }));

        results.AddRange(sector.Systems
            .SelectMany(system => system.Worlds.Select(world => (system, world)))
            .Where(item => Matches(normalizedQuery, item.world.Name, item.world.WorldType, item.world.AtmosphereType, item.system.Name, item.world.Id.ToString()))
            .OrderBy(item => item.world.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.world.Id)
            .Select(item => new StarWinSearchResult
            {
                Type = StarWinSearchResultType.World,
                Title = item.world.Name,
                Subtitle = item.system.Name,
                Tab = "Worlds",
                SectorId = sectorId,
                SystemId = item.system.Id,
                WorldId = item.world.Id
            }));

        results.AddRange(sector.Systems
            .SelectMany(system => system.Worlds
                .Where(world => world.Colony is not null)
                .Select(world => (system, world, colony: world.Colony!)))
            .Where(item => Matches(
                normalizedQuery,
                item.colony.Name,
                item.colony.ColonyClass,
                item.colony.AllegianceName,
                item.colony.ColonistRaceName,
                item.world.Name,
                item.colony.Id.ToString()))
            .OrderBy(item => item.world.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.colony.Id)
            .Select(item => new StarWinSearchResult
            {
                Type = StarWinSearchResultType.Colony,
                Title = string.IsNullOrWhiteSpace(item.colony.Name) ? item.colony.ColonyClass : item.colony.Name,
                Subtitle = item.world.Name,
                Tab = "Colonies",
                SectorId = sectorId,
                SystemId = item.system.Id,
                WorldId = item.world.Id,
                ColonyId = item.colony.Id,
                RaceId = item.colony.RaceId,
                EmpireId = item.colony.ControllingEmpireId
            }));

        results.AddRange(sector.Systems
            .SelectMany(system => system.SpaceHabitats.Select(habitat => (system, habitat)))
            .Where(item => Matches(normalizedQuery, item.habitat.Name, item.system.Name, item.habitat.Id.ToString()))
            .OrderBy(item => item.habitat.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.habitat.Id)
            .Select(item => new StarWinSearchResult
            {
                Type = StarWinSearchResultType.SpaceHabitat,
                Title = item.habitat.Name,
                Subtitle = item.system.Name,
                Tab = "Worlds",
                SectorId = sectorId,
                SystemId = item.system.Id,
                SpaceHabitatId = item.habitat.Id,
                EmpireId = item.habitat.ControlledByEmpireId
            }));

        return Task.FromResult<IReadOnlyList<StarWinSearchResult>>(results.Take(maxResults).ToList());
    }

    public Task<int?> ResolveSystemIdAsync(
        int sectorId,
        int? worldId = null,
        int? colonyId = null,
        int? habitatId = null,
        CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null)
        {
            return Task.FromResult<int?>(null);
        }

        if (colonyId is int requestedColonyId)
        {
            var colonySystemId = sector.Systems
                .FirstOrDefault(system => system.Worlds.Any(world => world.Colony?.Id == requestedColonyId))
                ?.Id;
            if (colonySystemId.HasValue)
            {
                return Task.FromResult<int?>(colonySystemId.Value);
            }
        }

        if (habitatId is int requestedHabitatId)
        {
            var habitatSystemId = sector.Systems
                .FirstOrDefault(system => system.SpaceHabitats.Any(habitat => habitat.Id == requestedHabitatId))
                ?.Id;
            if (habitatSystemId.HasValue)
            {
                return Task.FromResult<int?>(habitatSystemId.Value);
            }
        }

        if (worldId is int requestedWorldId)
        {
            var worldSystemId = sector.Systems
                .FirstOrDefault(system => system.Worlds.Any(world => world.Id == requestedWorldId))
                ?.Id;
            if (worldSystemId.HasValue)
            {
                return Task.FromResult<int?>(worldSystemId.Value);
            }
        }

        return Task.FromResult<int?>(null);
    }

    public Task<IReadOnlyList<ExplorerLookupOption>> LoadSectorEmpireOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null)
        {
            return Task.FromResult<IReadOnlyList<ExplorerLookupOption>>([]);
        }

        var empireIds = GetEmpireIds(sector);
        return Task.FromResult<IReadOnlyList<ExplorerLookupOption>>(context.Empires
            .Where(empire => empireIds.Contains(empire.Id))
            .OrderBy(empire => empire.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(empire => empire.Id)
            .Select(empire => new ExplorerLookupOption(empire.Id, empire.Name))
            .ToList());
    }

    public async Task<ExplorerSystemFilterOptions> LoadSystemFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        return new ExplorerSystemFilterOptions(await LoadSectorEmpireOptionsAsync(sectorId, cancellationToken));
    }

    public Task<ExplorerSystemListPage> LoadSystemListPageAsync(ExplorerSystemListPageRequest request, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(request.SectorId);
        if (sector is null)
        {
            return Task.FromResult(new ExplorerSystemListPage([], false));
        }

        var query = sector.Systems.AsEnumerable();
        if (request.EmpireId is int empireId)
        {
            query = query.Where(system => system.AllegianceId == empireId);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var normalizedQuery = request.Query.Trim();
            query = query.Where(system => Matches(
                normalizedQuery,
                system.Name,
                system.Id.ToString(),
                system.LegacySystemId?.ToString(),
                GetEmpireName(system.AllegianceId),
                system.AstralBodies.Select(body => body.Classification).ToArray(),
                system.AstralBodies.Select(body => body.Role.ToString()).ToArray(),
                system.AstralBodies.Select(body => body.Kind.ToString()).ToArray(),
                system.Worlds.Select(world => world.Name).ToArray(),
                system.Worlds.Select(world => world.WorldType).ToArray()));
        }

        var items = query
            .OrderBy(system => system.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(system => system.Id)
            .Select(MapSystemListItem)
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToList();

        var hasMore = items.Count > request.Limit;
        return Task.FromResult(new ExplorerSystemListPage(items.Take(request.Limit).ToList(), hasMore));
    }

    public Task<ExplorerSystemListItem?> LoadSystemListItemAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        var system = sector?.Systems.FirstOrDefault(item => item.Id == systemId);
        return Task.FromResult(system is null ? null : MapSystemListItem(system));
    }

    public Task<ExplorerSystemDetail?> LoadSystemDetailAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        var system = sector?.Systems.FirstOrDefault(item => item.Id == systemId);
        return Task.FromResult(system is null ? null : new ExplorerSystemDetail(sectorId, system));
    }

    public async Task<ExplorerWorldsWorkspace?> LoadWorldsWorkspaceAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        var system = sector?.Systems.FirstOrDefault(item => item.Id == systemId);
        if (system is null)
        {
            return null;
        }

        return new ExplorerWorldsWorkspace(sectorId, system, await LoadSectorEmpireOptionsAsync(sectorId, cancellationToken));
    }

    public Task<ExplorerColonyFilterOptions> LoadColonyFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null)
        {
            return Task.FromResult(new ExplorerColonyFilterOptions([], [], []));
        }

        var colonyWorlds = sector.Systems
            .SelectMany(system => system.Worlds)
            .Where(world => world.Colony is not null)
            .ToList();

        var colonyRaceIds = colonyWorlds
            .SelectMany(world => world.Colony is null
                ? []
                : world.Colony.Demographics.Count == 0
                    ? [world.Colony.RaceId]
                    : world.Colony.Demographics.Select(demographic => (int)demographic.RaceId))
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();

        var races = context.AlienRaces
            .Where(race => colonyRaceIds.Contains(race.Id))
            .OrderBy(race => race.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(race => race.Id)
            .Select(race => new ExplorerLookupOption(race.Id, race.Name))
            .ToList();

        var empireIds = GetEmpireIds(sector);
        var empires = context.Empires
            .Where(empire => empireIds.Contains(empire.Id))
            .OrderBy(empire => empire.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(empire => empire.Id)
            .Select(empire => new ExplorerLookupOption(empire.Id, empire.Name))
            .ToList();

        var classes = colonyWorlds
            .Select(world => world.Colony?.ColonyClass)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(new ExplorerColonyFilterOptions(races, empires, classes));
    }

    public Task<ExplorerColonyListPage> LoadColonyListPageAsync(ExplorerColonyListPageRequest request, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(request.SectorId);
        if (sector is null)
        {
            return Task.FromResult(new ExplorerColonyListPage([], false));
        }

        var query = sector.Systems
            .SelectMany(system => system.Worlds
                .Where(world => world.Colony is not null)
                .Select(world => new { System = system, World = world, Colony = world.Colony! }))
            .AsEnumerable();

        if (request.RaceId is int raceId)
        {
            query = query.Where(item =>
                item.Colony.RaceId == raceId
                || item.Colony.Demographics.Any(demographic => demographic.RaceId == raceId));
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
            query = query.Where(item => string.Equals(item.Colony.ColonyClass, colonyClass, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var normalizedQuery = request.Query.Trim();
            query = query.Where(item => Matches(
                normalizedQuery,
                item.World.Name,
                item.World.WorldType,
                item.Colony.Name,
                item.Colony.ColonyClass,
                item.Colony.ColonistRaceName,
                item.Colony.AllegianceName,
                item.Colony.Starport,
                item.Colony.GovernmentType,
                item.Colony.ExportResource,
                item.Colony.ImportResource,
                item.Colony.Id.ToString(),
                item.Colony.WorldId.ToString(),
                item.Colony.Demographics.Select(demographic => demographic.RaceName).ToArray()));
        }

        var items = query
            .OrderByDescending(item => item.Colony.EstimatedPopulation)
            .ThenBy(item => item.World.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Colony.Id)
            .Select(item => new ExplorerColonyListItem(
                item.Colony.Id,
                item.World.Id,
                item.System.Id,
                string.IsNullOrWhiteSpace(item.Colony.Name) ? item.Colony.ColonyClass : item.Colony.Name,
                item.World.Name,
                item.Colony.AllegianceName,
                item.Colony.EstimatedPopulation))
            .Skip(request.Offset)
            .Take(request.Limit + 1)
            .ToList();

        var hasMore = items.Count > request.Limit;
        return Task.FromResult(new ExplorerColonyListPage(items.Take(request.Limit).ToList(), hasMore));
    }

    public Task<ExplorerColonyListItem?> LoadColonyListItemAsync(int sectorId, int colonyId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        var match = sector?.Systems
            .SelectMany(system => system.Worlds
                .Where(world => world.Colony?.Id == colonyId)
                .Select(world => new { System = system, World = world, Colony = world.Colony! }))
            .FirstOrDefault();

        return Task.FromResult(match is null
            ? null
            : new ExplorerColonyListItem(
                match.Colony.Id,
                match.World.Id,
                match.System.Id,
                string.IsNullOrWhiteSpace(match.Colony.Name) ? match.Colony.ColonyClass : match.Colony.Name,
                match.World.Name,
                match.Colony.AllegianceName,
                match.Colony.EstimatedPopulation));
    }

    public Task<ExplorerColonyDetail?> LoadColonyDetailAsync(int sectorId, int colonyId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        var match = sector?.Systems
            .SelectMany(system => system.Worlds
                .Where(world => world.Colony?.Id == colonyId)
                .Select(world => new { World = world, Colony = world.Colony! }))
            .FirstOrDefault();

        return Task.FromResult(match is null ? null : new ExplorerColonyDetail(sectorId, match.Colony, match.World));
    }

    public Task<ExplorerHyperlaneWorkspace?> LoadHyperlaneWorkspaceAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        var sector = GetSector(sectorId);
        if (sector is null)
        {
            return Task.FromResult<ExplorerHyperlaneWorkspace?>(null);
        }

        var systems = sector.Systems
            .OrderBy(system => system.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(system => system.Id)
            .Select(system => new ExplorerHyperlaneSystem(
                system.Id,
                system.LegacySystemId,
                system.Name,
                system.Coordinates,
                system.AllegianceId))
            .ToList();

        var savedRoutes = sector.SavedRoutes
            .OrderBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ThenBy(route => route.Id)
            .Select(CloneRoute)
            .ToList();

        return Task.FromResult<ExplorerHyperlaneWorkspace?>(new ExplorerHyperlaneWorkspace(
            sector.Id,
            sector.Name,
            sector.Configuration ?? new SectorConfiguration { SectorId = sector.Id },
            systems,
            savedRoutes,
            systems.Select(system => system.SystemId).ToList(),
            context.Empires
                .OrderBy(empire => empire.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(empire => empire.Id)
                .Select(empire => new ExplorerLookupOption(empire.Id, empire.Name))
                .ToList()));
    }

    public Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerAlienRaceDetail?> LoadAlienRaceDetailAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerReligionFilterOptions> LoadReligionFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerReligionListPage> LoadReligionListPageAsync(ExplorerReligionListPageRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerReligionListItem?> LoadReligionListItemAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerReligionDetail?> LoadReligionDetailAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    private static SectorSavedRoute CloneRoute(SectorSavedRoute route)
    {
        return new SectorSavedRoute
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
        };
    }

    private static bool Matches(string query, params object?[] values)
    {
        foreach (var value in values)
        {
            switch (value)
            {
                case null:
                    continue;
                case string text when !string.IsNullOrWhiteSpace(text)
                    && text.Contains(query, StringComparison.OrdinalIgnoreCase):
                    return true;
                case IEnumerable<string?> valueSet:
                    foreach (var text in valueSet)
                    {
                        if (!string.IsNullOrWhiteSpace(text)
                            && text.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    break;
            }
        }

        return false;
    }

    private HashSet<int> GetRaceIds(StarWinSector sector)
    {
        return sector.Systems
            .SelectMany(system => system.Worlds)
            .Where(world => world.Colony is not null)
            .SelectMany(world => world.Colony!.Demographics.Count == 0
                ? [world.Colony.RaceId]
                : world.Colony.Demographics.Select(demographic => (int)demographic.RaceId))
            .Where(id => id > 0)
            .ToHashSet();
    }

    private HashSet<int> GetEmpireIds(StarWinSector sector)
    {
        var empireIds = new HashSet<int>();

        foreach (var system in sector.Systems)
        {
            if (system.AllegianceId > 0 && system.AllegianceId != ushort.MaxValue)
            {
                empireIds.Add(system.AllegianceId);
            }

            foreach (var world in system.Worlds)
            {
                if (world.Colony is not Colony colony)
                {
                    continue;
                }

                AddEmpireId(empireIds, colony.AllegianceId);
                AddEmpireId(empireIds, colony.ControllingEmpireId);
                AddEmpireId(empireIds, colony.FoundingEmpireId);
                AddEmpireId(empireIds, colony.ParentEmpireId);
            }

            foreach (var habitat in system.SpaceHabitats)
            {
                AddEmpireId(empireIds, habitat.ControlledByEmpireId);
                AddEmpireId(empireIds, habitat.BuiltByEmpireId);
            }
        }

        return empireIds;
    }

    private static void AddEmpireId(ISet<int> empireIds, int? empireId)
    {
        if (empireId is > 0 and not ushort.MaxValue)
        {
            empireIds.Add(empireId.Value);
        }
    }

    private StarWinSector? GetSector(int sectorId)
    {
        return context.Sectors.FirstOrDefault(sector => sector.Id == sectorId);
    }

    private ExplorerSystemListItem MapSystemListItem(StarSystem system)
    {
        return new ExplorerSystemListItem(
            system.Id,
            system.LegacySystemId,
            system.Name,
            system.Coordinates,
            system.AllegianceId,
            GetEmpireName(system.AllegianceId));
    }

    private string GetEmpireName(int? empireId)
    {
        if (empireId is not > 0 || empireId == ushort.MaxValue)
        {
            return "Independent";
        }

        return context.Empires.FirstOrDefault(empire => empire.Id == empireId)?.Name
            ?? empireId.Value.ToString();
    }
}
