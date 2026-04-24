using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Domain.Services;

public static class SectorRoutePlanner
{
    private const double LightYearsPerParsec = 3.26156d;

    public static IReadOnlyList<SectorHyperlaneRouteDefinition> BuildHyperlaneRoutes(
        StarWinSector sector,
        IReadOnlyDictionary<int, Empire> empiresById,
        Action<SectorHyperlaneRouteBuildProgress>? progress = null)
    {
        return BuildHyperlaneRouteGeneration(sector, empiresById, progress).Routes;
    }

    public static SectorHyperlaneRouteGenerationResult BuildHyperlaneRouteGeneration(
        StarWinSector sector,
        IReadOnlyDictionary<int, Empire> empiresById,
        Action<SectorHyperlaneRouteBuildProgress>? progress = null)
    {
        if (sector.Systems.Count < 2)
        {
            return new SectorHyperlaneRouteGenerationResult([], SectorHyperlaneNetworkReport.Empty);
        }

        var eligibleSystems = GetEligibleSystems(sector, empiresById);
        if (eligibleSystems.Count < 2)
        {
            progress?.Invoke(new SectorHyperlaneRouteBuildProgress(eligibleSystems.Count, eligibleSystems.Count, 0));
            return new SectorHyperlaneRouteGenerationResult(
                [],
                new SectorHyperlaneNetworkReport(0, [], eligibleSystems.Count, eligibleSystems.Count, 0));
        }

        var candidates = new List<SectorHyperlaneRouteDefinition>();
        progress?.Invoke(new SectorHyperlaneRouteBuildProgress(0, eligibleSystems.Count, 0));

        for (var sourceIndex = 0; sourceIndex < eligibleSystems.Count; sourceIndex++)
        {
            var source = eligibleSystems[sourceIndex];
            for (var targetIndex = sourceIndex + 1; targetIndex < eligibleSystems.Count; targetIndex++)
            {
                var target = eligibleSystems[targetIndex];
                var distanceParsecs = CalculateParsecDistance(source.System.Coordinates, target.System.Coordinates);
                if ((source.MaximumReachParsecs >= 0 && distanceParsecs > source.MaximumReachParsecs)
                    || (target.MaximumReachParsecs >= 0 && distanceParsecs > target.MaximumReachParsecs))
                {
                    continue;
                }

                if (!TryResolveHyperlaneRule(
                        source.System,
                        source.Empires,
                        target.System,
                        target.Empires,
                        sector.Configuration,
                        out var resolvedTier,
                        out var ownership))
                {
                    continue;
                }

                var maximumDistanceParsecs = GetMaximumDistanceParsecs(sector.Configuration, resolvedTier);
                if (maximumDistanceParsecs >= 0 && distanceParsecs > maximumDistanceParsecs)
                {
                    continue;
                }

                candidates.Add(new SectorHyperlaneRouteDefinition(
                    source.System.Id,
                    target.System.Id,
                    distanceParsecs,
                    CalculateHyperlaneTravelTimeYears(sector.Configuration, resolvedTier, distanceParsecs),
                    resolvedTier,
                    GetTierName(sector.Configuration, resolvedTier),
                    ownership.PrimaryOwnerEmpireId,
                    ownership.PrimaryOwnerEmpireName,
                    ownership.SecondaryOwnerEmpireId,
                    ownership.SecondaryOwnerEmpireName));
            }

            progress?.Invoke(new SectorHyperlaneRouteBuildProgress(sourceIndex + 1, eligibleSystems.Count, candidates.Count));
        }

        var routes = ApplyConnectionLimits(candidates, eligibleSystems, sector.Configuration);
        var report = BuildHyperlaneNetworkReport(
            eligibleSystems.Select(item => item.System.Id),
            routes);

        return new SectorHyperlaneRouteGenerationResult(
            routes.OrderBy(route => route.SourceSystemId).ThenBy(route => route.TargetSystemId).ToList(),
            report);
    }

    public static SectorHyperlaneNetworkReport BuildHyperlaneNetworkReport(
        StarWinSector sector,
        IReadOnlyDictionary<int, Empire> empiresById,
        IEnumerable<SectorHyperlaneRouteDefinition> routes)
    {
        var eligibleSystemIds = GetEligibleSystems(sector, empiresById)
            .Select(item => item.System.Id)
            .ToList();

        return BuildHyperlaneNetworkReport(eligibleSystemIds, routes);
    }

    public static SectorHyperlaneNetworkReport BuildHyperlaneNetworkReport(
        IEnumerable<int> eligibleSystemIds,
        IEnumerable<SectorHyperlaneRouteDefinition> routes)
    {
        var eligibleIds = eligibleSystemIds.Distinct().OrderBy(id => id).ToList();
        if (eligibleIds.Count == 0)
        {
            return SectorHyperlaneNetworkReport.Empty;
        }

        var adjacency = eligibleIds.ToDictionary(id => id, _ => new HashSet<int>());
        foreach (var route in routes)
        {
            if (!adjacency.ContainsKey(route.SourceSystemId) || !adjacency.ContainsKey(route.TargetSystemId))
            {
                continue;
            }

            adjacency[route.SourceSystemId].Add(route.TargetSystemId);
            adjacency[route.TargetSystemId].Add(route.SourceSystemId);
        }

        var connectedSystemIds = adjacency
            .Where(item => item.Value.Count > 0)
            .Select(item => item.Key)
            .ToHashSet();
        var strandedSystemIds = eligibleIds
            .Where(id => !connectedSystemIds.Contains(id))
            .ToList();
        var visited = new HashSet<int>();
        var networkSizes = new List<int>();

        foreach (var systemId in connectedSystemIds.OrderBy(id => id))
        {
            if (!visited.Add(systemId))
            {
                continue;
            }

            var count = 0;
            var queue = new Queue<int>();
            queue.Enqueue(systemId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                count++;

                foreach (var neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            networkSizes.Add(count);
        }

        networkSizes.Sort((left, right) => right.CompareTo(left));
        return new SectorHyperlaneNetworkReport(
            networkSizes.Count,
            networkSizes,
            strandedSystemIds.Count,
            eligibleIds.Count,
            connectedSystemIds.Count);
    }

    public static bool TryResolveHyperlaneRule(
        StarSystem source,
        StarSystem target,
        SectorConfiguration configuration,
        IReadOnlyDictionary<int, Empire> empiresById,
        out int technologyLevel,
        out SectorHyperlaneOwnership ownership)
    {
        var sourceEmpires = GetSystemEmpires(source, empiresById).ToList();
        var targetEmpires = GetSystemEmpires(target, empiresById).ToList();
        return TryResolveHyperlaneRule(source, sourceEmpires, target, targetEmpires, configuration, out technologyLevel, out ownership);
    }

    public static double CalculateHyperlaneTravelTimeYears(
        SectorConfiguration configuration,
        int technologyLevel,
        double distanceParsecs)
    {
        var offLaneSpeed = GetOffLaneSpeedMultiplier(configuration, technologyLevel);
        var hyperlaneModifier = GetHyperlaneSpeedModifier(configuration, technologyLevel);
        return CalculateTravelTimeYears(distanceParsecs, offLaneSpeed * hyperlaneModifier);
    }

    public static double CalculateOffLaneTravelTimeYears(
        SectorConfiguration configuration,
        int technologyLevel,
        double distanceParsecs)
    {
        return CalculateTravelTimeYears(distanceParsecs, GetOffLaneSpeedMultiplier(configuration, technologyLevel));
    }

    public static double CalculateTravelTimeYears(double distanceParsecs, double speedMultiplier)
    {
        var effectiveSpeed = Math.Max(0.01d, speedMultiplier);
        return distanceParsecs * LightYearsPerParsec / effectiveSpeed;
    }

    public static double GetMaximumDistanceParsecs(SectorConfiguration configuration, int technologyLevel)
    {
        return technologyLevel switch
        {
            6 => (double)configuration.Tl6MaximumDistanceParsecs,
            7 => (double)configuration.Tl7MaximumDistanceParsecs,
            8 => (double)configuration.Tl8MaximumDistanceParsecs,
            9 => (double)configuration.Tl9MaximumDistanceParsecs,
            10 => (double)configuration.Tl10MaximumDistanceParsecs,
            _ => 0d
        };
    }

    public static string GetTierName(SectorConfiguration configuration, int technologyLevel)
    {
        return technologyLevel switch
        {
            6 => configuration.Tl6HyperlaneName,
            7 => configuration.Tl7HyperlaneName,
            8 => configuration.Tl8HyperlaneName,
            9 => configuration.Tl9HyperlaneName,
            10 => configuration.Tl10HyperlaneName,
            _ => "Unknown Hyperlane"
        };
    }

    public static double GetOffLaneSpeedMultiplier(SectorConfiguration configuration, int technologyLevel)
    {
        return technologyLevel switch
        {
            6 => (double)configuration.Tl6OffLaneSpeedMultiplier,
            7 => (double)configuration.Tl7OffLaneSpeedMultiplier,
            8 => (double)configuration.Tl8OffLaneSpeedMultiplier,
            9 => (double)configuration.Tl9OffLaneSpeedMultiplier,
            10 => (double)configuration.Tl10OffLaneSpeedMultiplier,
            _ => (double)configuration.Tl6OffLaneSpeedMultiplier
        };
    }

    public static double GetHyperlaneSpeedModifier(SectorConfiguration configuration, int technologyLevel)
    {
        return technologyLevel switch
        {
            6 => (double)configuration.Tl6HyperlaneSpeedModifier,
            7 => (double)configuration.Tl7HyperlaneSpeedModifier,
            8 => (double)configuration.Tl8HyperlaneSpeedModifier,
            9 => (double)configuration.Tl9HyperlaneSpeedModifier,
            10 => (double)configuration.Tl10HyperlaneSpeedModifier,
            _ => (double)configuration.Tl6HyperlaneSpeedModifier
        };
    }

    public static double CalculateParsecDistance(Coordinates origin, Coordinates target)
    {
        var deltaX = origin.XParsecs - target.XParsecs;
        var deltaY = origin.YParsecs - target.YParsecs;
        var deltaZ = origin.ZParsecs - target.ZParsecs;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
    }

    private static IReadOnlyList<HyperlaneEligibleSystem> GetEligibleSystems(
        StarWinSector sector,
        IReadOnlyDictionary<int, Empire> empiresById)
    {
        return sector.Systems
            .Select(system =>
            {
                var empires = GetSystemEmpires(system, empiresById).ToList();
                return new HyperlaneEligibleSystem(system, empires, GetMaximumReachParsecs(sector.Configuration, empires));
            })
            .Where(item => item.Empires.Any(empire => empire.CivilizationProfile.TechLevel >= 6))
            .OrderBy(item => item.System.Id)
            .ToList();
    }

    private static bool TryResolveHyperlaneRule(
        StarSystem source,
        IReadOnlyList<Empire> sourceEmpires,
        StarSystem target,
        IReadOnlyList<Empire> targetEmpires,
        SectorConfiguration configuration,
        out int technologyLevel,
        out SectorHyperlaneOwnership ownership)
    {
        technologyLevel = 0;
        ownership = SectorHyperlaneOwnership.None;

        if (sourceEmpires.Count == 0 || targetEmpires.Count == 0)
        {
            return false;
        }

        var sharedEmpires = sourceEmpires
            .Join(targetEmpires, sourceEmpire => sourceEmpire.Id, targetEmpire => targetEmpire.Id, (sourceEmpire, _) => sourceEmpire)
            .OrderByDescending(empire => empire.CivilizationProfile.TechLevel)
            .ThenBy(empire => empire.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sharedEmpire = sharedEmpires.FirstOrDefault();
        if (sharedEmpire is not null && sharedEmpire.CivilizationProfile.TechLevel >= 6)
        {
            var resolvedTechnologyLevel = sharedEmpire.CivilizationProfile.TechLevel;
            technologyLevel = resolvedTechnologyLevel;
            var additionalSharedEmpire = sharedEmpires
                .Skip(1)
                .FirstOrDefault(empire => empire.CivilizationProfile.TechLevel == resolvedTechnologyLevel);

            ownership = new SectorHyperlaneOwnership(
                sharedEmpire.Id,
                sharedEmpire.Name,
                additionalSharedEmpire?.Id,
                additionalSharedEmpire?.Name ?? string.Empty);
            return true;
        }

        Empire? chosenSourceEmpire = null;
        Empire? chosenTargetEmpire = null;
        var bestTier = 0;
        var bestUpperTier = 0;
        foreach (var sourceEmpire in sourceEmpires)
        {
            foreach (var targetEmpire in targetEmpires)
            {
                var lowerTier = Math.Min(sourceEmpire.CivilizationProfile.TechLevel, targetEmpire.CivilizationProfile.TechLevel);
                var upperTier = Math.Max(sourceEmpire.CivilizationProfile.TechLevel, targetEmpire.CivilizationProfile.TechLevel);
                if (lowerTier < 6)
                {
                    continue;
                }

                if (lowerTier > bestTier
                    || (lowerTier == bestTier && upperTier > bestUpperTier)
                    || (lowerTier == bestTier
                        && upperTier == bestUpperTier
                        && string.Compare(sourceEmpire.Name, chosenSourceEmpire?.Name, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    bestTier = lowerTier;
                    bestUpperTier = upperTier;
                    chosenSourceEmpire = sourceEmpire;
                    chosenTargetEmpire = targetEmpire;
                }
            }
        }

        if (chosenSourceEmpire is null || chosenTargetEmpire is null)
        {
            return false;
        }

        technologyLevel = bestTier;
        ownership = new SectorHyperlaneOwnership(
            chosenSourceEmpire.Id,
            chosenSourceEmpire.Name,
            chosenSourceEmpire.Id == chosenTargetEmpire.Id ? null : chosenTargetEmpire.Id,
            chosenSourceEmpire.Id == chosenTargetEmpire.Id ? string.Empty : chosenTargetEmpire.Name);
        return true;
    }

    private static IEnumerable<Empire> GetSystemEmpires(
        StarSystem system,
        IReadOnlyDictionary<int, Empire> empiresById)
    {
        var empireIds = system.Worlds
            .Where(world => world.Colony is not null)
            .SelectMany(world => GetColonyEmpireIds(world.Colony!))
            .Append(system.AllegianceId == ushort.MaxValue ? 0 : system.AllegianceId)
            .Where(empireId => empireId > 0)
            .Distinct();

        foreach (var empireId in empireIds)
        {
            if (empiresById.TryGetValue(empireId, out var empire))
            {
                yield return empire;
            }
        }
    }

    private static IEnumerable<int> GetColonyEmpireIds(Colony colony)
    {
        if (colony.ControllingEmpireId is int controllingEmpireId && controllingEmpireId > 0)
        {
            yield return controllingEmpireId;
        }

        if (colony.AllegianceId != ushort.MaxValue && colony.AllegianceId > 0)
        {
            yield return colony.AllegianceId;
        }
    }

    private static IReadOnlyList<SectorHyperlaneRouteDefinition> ApplyConnectionLimits(
        IReadOnlyList<SectorHyperlaneRouteDefinition> candidates,
        IReadOnlyList<HyperlaneEligibleSystem> eligibleSystems,
        SectorConfiguration configuration)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var selectedRoutes = new List<SectorHyperlaneRouteDefinition>();
        var selectedRouteKeys = new HashSet<(int SourceSystemId, int TargetSystemId)>();
        var statesBySystemId = eligibleSystems.ToDictionary(
            item => item.System.Id,
            _ => new ConnectionState());
        var maximumConnections = Math.Max(0, configuration.Tl9AndBelowMaximumConnectionsPerSystem);
        var reservedCrossEmpireConnections = Math.Max(0, configuration.AdditionalCrossEmpireConnectionsPerSystem);
        var candidateCountsBySystemId = candidates
            .SelectMany(route => new[] { route.SourceSystemId, route.TargetSystemId })
            .GroupBy(systemId => systemId)
            .ToDictionary(group => group.Key, group => group.Count());
        var firstPassLimit = maximumConnections <= 1
            ? maximumConnections
            : Math.Max(1, maximumConnections / 2);

        var firstPassCandidates = candidates
            .OrderBy(route => GetCandidateScarcity(candidateCountsBySystemId, route))
            .ThenBy(route => IsCrossEmpireRoute(route))
            .ThenByDescending(route => route.TechnologyLevel)
            .ThenBy(route => route.DistanceParsecs)
            .ThenBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ToList();
        var secondPassCandidates = candidates
            .OrderBy(route => GetCandidateScarcity(candidateCountsBySystemId, route))
            .ThenByDescending(route => route.TechnologyLevel)
            .ThenBy(route => route.DistanceParsecs)
            .ThenBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ToList();

        foreach (var candidate in firstPassCandidates)
        {
            if (candidate.TechnologyLevel >= 10)
            {
                AddSelectedRoute(selectedRoutes, selectedRouteKeys, candidate);
                continue;
            }

            if (TryAllocateRoute(
                    candidate,
                    firstPassLimit,
                    reservedCrossEmpireConnections,
                    allowReservedCrossEmpireConnections: false,
                    statesBySystemId))
            {
                AddSelectedRoute(selectedRoutes, selectedRouteKeys, candidate);
            }
        }

        foreach (var candidate in secondPassCandidates)
        {
            if (selectedRouteKeys.Contains(GetRouteKey(candidate)))
            {
                continue;
            }

            if (candidate.TechnologyLevel >= 10)
            {
                AddSelectedRoute(selectedRoutes, selectedRouteKeys, candidate);
                continue;
            }

            if (TryAllocateRoute(
                    candidate,
                    maximumConnections,
                    reservedCrossEmpireConnections,
                    allowReservedCrossEmpireConnections: true,
                    statesBySystemId))
            {
                AddSelectedRoute(selectedRoutes, selectedRouteKeys, candidate);
            }
        }

        ApplyStrandedSystemRescuePass(
            secondPassCandidates,
            eligibleSystems.Select(item => item.System.Id).ToList(),
            selectedRoutes,
            selectedRouteKeys,
            statesBySystemId,
            maximumConnections,
            reservedCrossEmpireConnections);

        return selectedRoutes;
    }

    private static void ApplyStrandedSystemRescuePass(
        IReadOnlyList<SectorHyperlaneRouteDefinition> candidates,
        IReadOnlyList<int> eligibleSystemIds,
        ICollection<SectorHyperlaneRouteDefinition> selectedRoutes,
        ISet<(int SourceSystemId, int TargetSystemId)> selectedRouteKeys,
        IDictionary<int, ConnectionState> statesBySystemId,
        int maximumConnections,
        int reservedCrossEmpireConnections)
    {
        var strandedSystemIds = BuildHyperlaneNetworkReport(eligibleSystemIds, selectedRoutes).StrandedSystemCount == 0
            ? []
            : eligibleSystemIds
                .Where(systemId => selectedRoutes.All(route => route.SourceSystemId != systemId && route.TargetSystemId != systemId))
                .OrderBy(systemId => systemId)
                .ToList();
        var degreeBySystemId = selectedRoutes
            .SelectMany(route => new[] { route.SourceSystemId, route.TargetSystemId })
            .GroupBy(systemId => systemId)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var strandedSystemId in strandedSystemIds)
        {
            var routeCandidates = candidates
                .Where(route => !selectedRouteKeys.Contains(GetRouteKey(route)))
                .Where(route => route.SourceSystemId == strandedSystemId || route.TargetSystemId == strandedSystemId)
                .ToList();
            if (routeCandidates.Count == 0)
            {
                continue;
            }

            if (routeCandidates.Any(route => CanAllocateNormally(GetNeighborSystemId(route, strandedSystemId), reservedCrossEmpireConnections, maximumConnections, statesBySystemId)))
            {
                continue;
            }

            var rescueCandidate = routeCandidates
                .OrderByDescending(route => degreeBySystemId.TryGetValue(GetNeighborSystemId(route, strandedSystemId), out var degree) && degree > 0)
                .ThenBy(route => IsCrossEmpireRoute(route))
                .ThenByDescending(route => route.TechnologyLevel)
                .ThenBy(route => route.DistanceParsecs)
                .ThenBy(route => route.SourceSystemId)
                .ThenBy(route => route.TargetSystemId)
                .FirstOrDefault(route =>
                {
                    var neighborSystemId = GetNeighborSystemId(route, strandedSystemId);
                    return statesBySystemId.TryGetValue(neighborSystemId, out var neighborState)
                        && neighborState.RescueOverrideCount == 0;
                });
            if (rescueCandidate is null)
            {
                continue;
            }

            if (!TryAllocateStrandedSide(
                    rescueCandidate,
                    strandedSystemId,
                    maximumConnections,
                    reservedCrossEmpireConnections,
                    statesBySystemId))
            {
                continue;
            }

            var rescueNeighborId = GetNeighborSystemId(rescueCandidate, strandedSystemId);
            statesBySystemId[rescueNeighborId].RescueOverrideCount++;
            AddSelectedRoute(selectedRoutes, selectedRouteKeys, rescueCandidate);
            degreeBySystemId[strandedSystemId] = degreeBySystemId.TryGetValue(strandedSystemId, out var currentStrandedDegree)
                ? currentStrandedDegree + 1
                : 1;
            degreeBySystemId[rescueNeighborId] = degreeBySystemId.TryGetValue(rescueNeighborId, out var currentNeighborDegree)
                ? currentNeighborDegree + 1
                : 1;
        }
    }

    private static bool TryAllocateRoute(
        SectorHyperlaneRouteDefinition candidate,
        int maximumConnections,
        int reservedCrossEmpireConnections,
        bool allowReservedCrossEmpireConnections,
        IDictionary<int, ConnectionState> statesBySystemId)
    {
        var isCrossEmpire = IsCrossEmpireRoute(candidate);
        var sourceAllocation = TryAllocateConnection(
            candidate.SourceSystemId,
            isCrossEmpire,
            maximumConnections,
            reservedCrossEmpireConnections,
            allowReservedCrossEmpireConnections,
            statesBySystemId);
        if (sourceAllocation == ConnectionAllocation.None)
        {
            return false;
        }

        var targetAllocation = TryAllocateConnection(
            candidate.TargetSystemId,
            isCrossEmpire,
            maximumConnections,
            reservedCrossEmpireConnections,
            allowReservedCrossEmpireConnections,
            statesBySystemId);
        if (targetAllocation == ConnectionAllocation.None)
        {
            ReleaseConnection(candidate.SourceSystemId, sourceAllocation, isCrossEmpire, statesBySystemId);
            return false;
        }

        return true;
    }

    private static bool TryAllocateStrandedSide(
        SectorHyperlaneRouteDefinition candidate,
        int strandedSystemId,
        int maximumConnections,
        int reservedCrossEmpireConnections,
        IDictionary<int, ConnectionState> statesBySystemId)
    {
        var isCrossEmpire = IsCrossEmpireRoute(candidate);
        return TryAllocateConnection(
                strandedSystemId,
                isCrossEmpire,
                maximumConnections,
                reservedCrossEmpireConnections,
                allowReservedCrossEmpireConnections: true,
                statesBySystemId)
            != ConnectionAllocation.None;
    }

    private static ConnectionAllocation TryAllocateConnection(
        int systemId,
        bool isCrossEmpire,
        int maximumConnections,
        int reservedCrossEmpireConnections,
        bool allowReservedCrossEmpireConnections,
        IDictionary<int, ConnectionState> statesBySystemId)
    {
        if (!statesBySystemId.TryGetValue(systemId, out var state))
        {
            return ConnectionAllocation.None;
        }

        if (state.StandardConnectionCount < maximumConnections)
        {
            state.StandardConnectionCount++;
            if (isCrossEmpire)
            {
                state.StandardCrossEmpireConnectionCount++;
            }

            return ConnectionAllocation.Standard;
        }

        if (!allowReservedCrossEmpireConnections
            || !isCrossEmpire
            || reservedCrossEmpireConnections <= 0
            || state.StandardCrossEmpireConnectionCount > 0
            || state.ReservedCrossEmpireConnectionCount >= reservedCrossEmpireConnections)
        {
            return ConnectionAllocation.None;
        }

        state.ReservedCrossEmpireConnectionCount++;
        return ConnectionAllocation.ReservedCrossEmpire;
    }

    private static void ReleaseConnection(
        int systemId,
        ConnectionAllocation allocation,
        bool isCrossEmpire,
        IDictionary<int, ConnectionState> statesBySystemId)
    {
        var state = statesBySystemId[systemId];
        if (allocation == ConnectionAllocation.Standard)
        {
            state.StandardConnectionCount = Math.Max(0, state.StandardConnectionCount - 1);
            if (isCrossEmpire)
            {
                state.StandardCrossEmpireConnectionCount = Math.Max(0, state.StandardCrossEmpireConnectionCount - 1);
            }

            return;
        }

        if (allocation == ConnectionAllocation.ReservedCrossEmpire)
        {
            state.ReservedCrossEmpireConnectionCount = Math.Max(0, state.ReservedCrossEmpireConnectionCount - 1);
        }
    }

    private static bool CanAllocateNormally(
        int systemId,
        int reservedCrossEmpireConnections,
        int maximumConnections,
        IDictionary<int, ConnectionState> statesBySystemId)
    {
        if (!statesBySystemId.TryGetValue(systemId, out var state))
        {
            return false;
        }

        return state.StandardConnectionCount < maximumConnections
            || (reservedCrossEmpireConnections > 0
                && state.StandardCrossEmpireConnectionCount == 0
                && state.ReservedCrossEmpireConnectionCount < reservedCrossEmpireConnections);
    }

    private static int GetNeighborSystemId(SectorHyperlaneRouteDefinition route, int strandedSystemId)
    {
        return route.SourceSystemId == strandedSystemId
            ? route.TargetSystemId
            : route.SourceSystemId;
    }

    private static double GetMaximumReachParsecs(
        SectorConfiguration configuration,
        IReadOnlyList<Empire> empires)
    {
        var maximumReach = 0d;
        foreach (var empire in empires.Where(empire => empire.CivilizationProfile.TechLevel >= 6))
        {
            var reach = GetMaximumDistanceParsecs(configuration, empire.CivilizationProfile.TechLevel);
            if (reach < 0)
            {
                return -1d;
            }

            maximumReach = Math.Max(maximumReach, reach);
        }

        return maximumReach;
    }

    private static void AddSelectedRoute(
        ICollection<SectorHyperlaneRouteDefinition> selectedRoutes,
        ISet<(int SourceSystemId, int TargetSystemId)> selectedRouteKeys,
        SectorHyperlaneRouteDefinition candidate)
    {
        if (selectedRouteKeys.Add(GetRouteKey(candidate)))
        {
            selectedRoutes.Add(candidate);
        }
    }

    private static (int SourceSystemId, int TargetSystemId) GetRouteKey(SectorHyperlaneRouteDefinition route)
    {
        return route.SourceSystemId <= route.TargetSystemId
            ? (route.SourceSystemId, route.TargetSystemId)
            : (route.TargetSystemId, route.SourceSystemId);
    }

    private static int GetCandidateScarcity(
        IReadOnlyDictionary<int, int> candidateCountsBySystemId,
        SectorHyperlaneRouteDefinition route)
    {
        var sourceCount = candidateCountsBySystemId.TryGetValue(route.SourceSystemId, out var sourceValue) ? sourceValue : int.MaxValue;
        var targetCount = candidateCountsBySystemId.TryGetValue(route.TargetSystemId, out var targetValue) ? targetValue : int.MaxValue;
        return Math.Min(sourceCount, targetCount);
    }

    private static bool IsCrossEmpireRoute(SectorHyperlaneRouteDefinition route)
    {
        return route.SecondaryOwnerEmpireId is int secondaryOwnerEmpireId
            && route.PrimaryOwnerEmpireId is int primaryOwnerEmpireId
            && secondaryOwnerEmpireId != primaryOwnerEmpireId;
    }

    private sealed record HyperlaneEligibleSystem(
        StarSystem System,
        IReadOnlyList<Empire> Empires,
        double MaximumReachParsecs);

    private sealed class ConnectionState
    {
        public int StandardConnectionCount { get; set; }

        public int StandardCrossEmpireConnectionCount { get; set; }

        public int ReservedCrossEmpireConnectionCount { get; set; }

        public int RescueOverrideCount { get; set; }
    }

    private enum ConnectionAllocation
    {
        None,
        Standard,
        ReservedCrossEmpire
    }
}

public sealed record SectorHyperlaneRouteDefinition(
    int SourceSystemId,
    int TargetSystemId,
    double DistanceParsecs,
    double TravelTimeYears,
    int TechnologyLevel,
    string TierName,
    int? PrimaryOwnerEmpireId,
    string PrimaryOwnerEmpireName,
    int? SecondaryOwnerEmpireId,
    string SecondaryOwnerEmpireName);

public sealed record SectorHyperlaneOwnership(
    int? PrimaryOwnerEmpireId,
    string PrimaryOwnerEmpireName,
    int? SecondaryOwnerEmpireId,
    string SecondaryOwnerEmpireName)
{
    public static SectorHyperlaneOwnership None { get; } = new(null, string.Empty, null, string.Empty);
}

public sealed record SectorHyperlaneRouteBuildProgress(
    int ProcessedSystems,
    int TotalSystems,
    int CandidateRouteCount);

public sealed record SectorHyperlaneRouteGenerationResult(
    IReadOnlyList<SectorHyperlaneRouteDefinition> Routes,
    SectorHyperlaneNetworkReport NetworkReport);

public sealed record SectorHyperlaneNetworkReport(
    int DistinctNetworkCount,
    IReadOnlyList<int> NetworkSizes,
    int StrandedSystemCount,
    int EligibleSystemCount,
    int ConnectedSystemCount)
{
    public static SectorHyperlaneNetworkReport Empty { get; } = new(0, [], 0, 0, 0);
}
