using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Domain.Services;

public static class SectorRoutePlanner
{
    private const double LightYearsPerParsec = 3.26156d;

    public static IReadOnlyList<SectorHyperlaneRouteDefinition> BuildHyperlaneRoutes(
        StarWinSector sector,
        IReadOnlyDictionary<int, Empire> empiresById)
    {
        if (sector.Systems.Count < 2)
        {
            return [];
        }

        var routesByPair = new Dictionary<(int SourceId, int TargetId), SectorHyperlaneRouteDefinition>();
        for (var sourceIndex = 0; sourceIndex < sector.Systems.Count; sourceIndex++)
        {
            var source = sector.Systems[sourceIndex];
            for (var targetIndex = sourceIndex + 1; targetIndex < sector.Systems.Count; targetIndex++)
            {
                var target = sector.Systems[targetIndex];
                var distanceParsecs = CalculateParsecDistance(source.Coordinates, target.Coordinates);
                if (!TryResolveHyperlaneRule(
                        source,
                        target,
                        sector.Configuration,
                        empiresById,
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

                routesByPair[(source.Id, target.Id)] = new SectorHyperlaneRouteDefinition(
                    source.Id,
                    target.Id,
                    distanceParsecs,
                    CalculateHyperlaneTravelTimeYears(sector.Configuration, resolvedTier, distanceParsecs),
                    resolvedTier,
                    GetTierName(sector.Configuration, resolvedTier),
                    ownership.PrimaryOwnerEmpireId,
                    ownership.PrimaryOwnerEmpireName,
                    ownership.SecondaryOwnerEmpireId,
                    ownership.SecondaryOwnerEmpireName);
            }
        }

        return routesByPair.Values
            .OrderBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ToList();
    }

    public static bool TryResolveHyperlaneRule(
        StarSystem source,
        StarSystem target,
        SectorConfiguration configuration,
        IReadOnlyDictionary<int, Empire> empiresById,
        out int technologyLevel,
        out SectorHyperlaneOwnership ownership)
    {
        technologyLevel = 0;
        ownership = SectorHyperlaneOwnership.None;

        var sourceEmpires = GetSystemEmpires(source, empiresById).ToList();
        var targetEmpires = GetSystemEmpires(target, empiresById).ToList();
        if (sourceEmpires.Count == 0 || targetEmpires.Count == 0)
        {
            return false;
        }

        var sharedEmpires = sourceEmpires
            .Join(targetEmpires, sourceEmpire => sourceEmpire.Id, targetEmpire => targetEmpire.Id, (sourceEmpire, _) => sourceEmpire)
            .OrderByDescending(empire => empire.CivilizationProfile.TechLevel)
            .ThenBy(empire => empire.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sharedEmpire = sharedEmpires
            .FirstOrDefault();
        if (sharedEmpire is not null && sharedEmpire.CivilizationProfile.TechLevel >= 6)
        {
            technologyLevel = sharedEmpire.CivilizationProfile.TechLevel;
            var sharedTechnologyLevel = technologyLevel;
            var additionalSharedEmpire = sharedEmpires
                .Skip(1)
                .FirstOrDefault(empire => empire.CivilizationProfile.TechLevel == sharedTechnologyLevel);

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
