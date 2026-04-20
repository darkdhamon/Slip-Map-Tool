using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Web.Components.Explorer;

public sealed class ExplorerSectorCacheBuilder
{
    private readonly Dictionary<int, ExplorerSectorCache> sectorCaches = [];

    public ExplorerSectorCache Get(StarWinSector sector)
    {
        if (sectorCaches.TryGetValue(sector.Id, out var cachedSector))
        {
            return cachedSector;
        }

        var sectorWorlds = sector.Systems
            .SelectMany(system => system.Worlds)
            .ToList();
        var sectorColonies = sectorWorlds
            .Where(world => world.Colony is not null)
            .Select(world => (World: world, Colony: world.Colony!))
            .ToList();
        var cache = new ExplorerSectorCache(
            Worlds: sectorWorlds,
            WorldsByName: sectorWorlds
                .OrderBy(world => world.Name)
                .ThenBy(world => world.Id)
                .ToList(),
            WorldsById: sectorWorlds.ToDictionary(world => world.Id),
            Colonies: sectorColonies,
            ColoniesByWorldName: sectorColonies
                .OrderBy(item => item.World.Name)
                .ThenBy(item => item.Colony.Id)
                .ToList(),
            ColoniesByPopulation: sectorColonies
                .OrderByDescending(item => item.Colony.EstimatedPopulation)
                .ThenBy(item => item.World.Name)
                .ToList(),
            ColoniesByColonyId: sectorColonies
                .GroupBy(item => item.Colony.Id)
                .ToDictionary(group => group.Key, group => group.First()),
            SystemsByName: sector.Systems
                .OrderBy(system => system.Name)
                .ThenBy(system => system.Id)
                .ToList(),
            SystemsById: sector.Systems.ToDictionary(system => system.Id),
            EventTypes: sector.History
                .Select(history => history.EventType)
                .Where(eventType => !string.IsNullOrWhiteSpace(eventType))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(eventType => eventType)
                .ToList(),
            SortedHistory: sector.History
                .OrderBy(history => history.Century)
                .ThenBy(history => history.EventType)
                .ThenBy(history => history.Description)
                .ToList(),
            RaceIds: GetSectorRaceIds(sector, sectorWorlds, sectorColonies),
            EmpireIds: GetSectorEmpireIds(sector, sectorWorlds, sectorColonies));

        sectorCaches[sector.Id] = cache;
        return cache;
    }

    private static HashSet<int> GetSectorRaceIds(
        StarWinSector sector,
        IReadOnlyList<World> sectorWorlds,
        IReadOnlyList<(World World, Colony Colony)> sectorColonies)
    {
        var raceIds = new HashSet<int>();

        foreach (var world in sectorWorlds)
        {
            if (world.AlienRaceId is int worldRaceId)
            {
                raceIds.Add(worldRaceId);
            }
        }

        foreach (var item in sectorColonies)
        {
            raceIds.Add(item.Colony.RaceId);
            foreach (var demographic in item.Colony.Demographics)
            {
                raceIds.Add(demographic.RaceId);
            }
        }

        foreach (var history in sector.History)
        {
            if (history.RaceId is int raceId)
            {
                raceIds.Add(raceId);
            }

            if (history.OtherRaceId is int otherRaceId)
            {
                raceIds.Add(otherRaceId);
            }
        }

        return raceIds;
    }

    private static HashSet<int> GetSectorEmpireIds(
        StarWinSector sector,
        IReadOnlyList<World> sectorWorlds,
        IReadOnlyList<(World World, Colony Colony)> sectorColonies)
    {
        var empireIds = new HashSet<int>();

        foreach (var system in sector.Systems)
        {
            if (system.AllegianceId != ushort.MaxValue)
            {
                empireIds.Add(system.AllegianceId);
            }

            foreach (var habitat in system.SpaceHabitats)
            {
                if (habitat.BuiltByEmpireId is int builtByEmpireId)
                {
                    empireIds.Add(builtByEmpireId);
                }

                if (habitat.ControlledByEmpireId is int controlledByEmpireId)
                {
                    empireIds.Add(controlledByEmpireId);
                }
            }
        }

        foreach (var world in sectorWorlds)
        {
            if (world.ControlledByEmpireId is int controlledByEmpireId)
            {
                empireIds.Add(controlledByEmpireId);
            }

            if (world.AllegianceId != ushort.MaxValue)
            {
                empireIds.Add(world.AllegianceId);
            }
        }

        foreach (var item in sectorColonies)
        {
            if (item.Colony.ControllingEmpireId is int controllingEmpireId)
            {
                empireIds.Add(controllingEmpireId);
            }

            if (item.Colony.FoundingEmpireId is int foundingEmpireId)
            {
                empireIds.Add(foundingEmpireId);
            }

            if (item.Colony.ParentEmpireId is int parentEmpireId)
            {
                empireIds.Add(parentEmpireId);
            }

            if (item.Colony.AllegianceId != ushort.MaxValue)
            {
                empireIds.Add(item.Colony.AllegianceId);
            }
        }

        foreach (var history in sector.History)
        {
            if (history.EmpireId is int empireId)
            {
                empireIds.Add(empireId);
            }
        }

        return empireIds;
    }
}

public sealed record ExplorerSectorCache(
    IReadOnlyList<World> Worlds,
    IReadOnlyList<World> WorldsByName,
    IReadOnlyDictionary<int, World> WorldsById,
    IReadOnlyList<(World World, Colony Colony)> Colonies,
    IReadOnlyList<(World World, Colony Colony)> ColoniesByWorldName,
    IReadOnlyList<(World World, Colony Colony)> ColoniesByPopulation,
    IReadOnlyDictionary<int, (World World, Colony Colony)> ColoniesByColonyId,
    IReadOnlyList<StarSystem> SystemsByName,
    IReadOnlyDictionary<int, StarSystem> SystemsById,
    IReadOnlyList<string> EventTypes,
    IReadOnlyList<HistoryEvent> SortedHistory,
    HashSet<int> RaceIds,
    HashSet<int> EmpireIds);
