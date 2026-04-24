using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinSectorRouteService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinSectorRouteService
{
    public async Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(
        int sectorId,
        IProgress<SectorRouteSaveProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        progress?.Report(new SectorRouteSaveProgress("Loading sector", "Reading the active sector and travel configuration.", 10));
        await Task.Yield();

        var sector = await dbContext.Sectors
            .AsSplitQuery()
            .Include(item => item.Configuration)
            .Include(item => item.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.Colony)
            .FirstOrDefaultAsync(item => item.Id == sectorId, cancellationToken)
            ?? throw new InvalidOperationException("Sector was not found.");

        progress?.Report(new SectorRouteSaveProgress("Loading empires", "Resolving empire technology levels and ownership data.", 25));
        await Task.Yield();

        var empiresById = await dbContext.Empires
            .AsNoTracking()
            .ToDictionaryAsync(empire => empire.Id, cancellationToken);

        progress?.Report(new SectorRouteSaveProgress("Generating routes", $"Scanning {sector.Systems.Count:N0} systems for TL6+ colony hyperlane endpoints.", 35, 0, sector.Systems.Count));
        await Task.Yield();

        var generation = await Task.Run(
            () => SectorRoutePlanner.BuildHyperlaneRouteGeneration(
                sector,
                empiresById,
                update =>
                {
                    var totalSystems = Math.Max(1, update.TotalSystems);
                    var percent = 35 + (int)Math.Round(update.ProcessedSystems * 35d / totalSystems, MidpointRounding.AwayFromZero);
                    progress?.Report(new SectorRouteSaveProgress(
                        "Generating routes",
                        $"Processed {update.ProcessedSystems:N0} of {update.TotalSystems:N0} TL6+ colony systems and found {update.CandidateRouteCount:N0} candidate hyperlane segment{(update.CandidateRouteCount == 1 ? string.Empty : "s")}.",
                        Math.Clamp(percent, 35, 70),
                        update.ProcessedSystems,
                        update.TotalSystems));
                }),
            cancellationToken);
        var generatedAtUtc = DateTime.UtcNow;

        progress?.Report(new SectorRouteSaveProgress("Preparing saved routes", $"Merging {generation.Routes.Count:N0} generated route segment{(generation.Routes.Count == 1 ? string.Empty : "s")} into the sector cache.", 75));
        await Task.Yield();

        var existingRoutes = await dbContext.SectorSavedRoutes
            .Where(route => route.SectorId == sectorId)
            .ToListAsync(cancellationToken);

        var replacedExistingRoutes = existingRoutes.Count > 0;
        var preservedRoutes = existingRoutes
            .Where(route => route.IsUserPersisted)
            .ToList();
        var preservedRouteKeys = preservedRoutes
            .Select(route => NormalizeRoutePair(route.SourceSystemId, route.TargetSystemId))
            .ToHashSet();
        var removableRoutes = existingRoutes
            .Where(route => !route.IsUserPersisted)
            .ToList();
        if (removableRoutes.Count > 0)
        {
            dbContext.SectorSavedRoutes.RemoveRange(removableRoutes);
        }

        progress?.Report(new SectorRouteSaveProgress("Writing database", "Saving the updated route cache to the database.", 90));
        await Task.Yield();

        var generatedRoutesToPersist = generation.Routes
            .Where(route => !preservedRouteKeys.Contains(NormalizeRoutePair(route.SourceSystemId, route.TargetSystemId)))
            .Select(route => CreateSavedRoute(sectorId, route, generatedAtUtc))
            .ToList();

        dbContext.SectorSavedRoutes.AddRange(generatedRoutesToPersist);

        var finalRoutes = preservedRoutes
            .Select(MapSavedRouteDefinition)
            .Concat(generatedRoutesToPersist.Select(MapSavedRouteDefinition))
            .OrderBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        var networkReport = SectorRoutePlanner.BuildHyperlaneNetworkReport(sector, empiresById, finalRoutes);
        progress?.Report(new SectorRouteSaveProgress("Routes saved", $"Stored {finalRoutes.Count:N0} cached hyperlane segment{(finalRoutes.Count == 1 ? string.Empty : "s")} for this sector.", 100));
        return new SectorRouteSaveResult(
            sectorId,
            finalRoutes.Count,
            generatedRoutesToPersist.Count,
            preservedRoutes.Count,
            replacedExistingRoutes,
            generatedAtUtc,
            networkReport);
    }

    public async Task<SectorSavedRoute> SaveManualRouteAsync(
        SectorManualRouteSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (request.SourceSystemId <= 0 || request.TargetSystemId <= 0)
        {
            throw new InvalidOperationException("Hyperlanes require valid source and target systems.");
        }

        if (request.SourceSystemId == request.TargetSystemId)
        {
            throw new InvalidOperationException("A hyperlane cannot connect a system to itself.");
        }

        if (request.TechnologyLevel is < 6 or > 10)
        {
            throw new InvalidOperationException("Manual hyperlanes must use TL6 through TL10.");
        }

        var sector = await dbContext.Sectors
            .Include(item => item.Configuration)
            .Include(item => item.Systems)
            .FirstOrDefaultAsync(item => item.Id == request.SectorId, cancellationToken)
            ?? throw new InvalidOperationException("Sector was not found.");

        var systemsById = sector.Systems.ToDictionary(system => system.Id);
        if (!systemsById.TryGetValue(request.SourceSystemId, out var sourceSystem)
            || !systemsById.TryGetValue(request.TargetSystemId, out var targetSystem))
        {
            throw new InvalidOperationException("Both systems must belong to the current sector.");
        }

        var normalizedPair = NormalizeRoutePair(request.SourceSystemId, request.TargetSystemId);
        var existingRoutes = await dbContext.SectorSavedRoutes
            .Where(route => route.SectorId == request.SectorId)
            .ToListAsync(cancellationToken);
        var duplicateRoute = existingRoutes.FirstOrDefault(route =>
            route.Id != request.RouteId
            && NormalizeRoutePair(route.SourceSystemId, route.TargetSystemId) == normalizedPair);
        if (duplicateRoute is not null)
        {
            throw new InvalidOperationException("Only one saved hyperlane may exist for a given system pair.");
        }

        var empiresById = await dbContext.Empires
            .AsNoTracking()
            .ToDictionaryAsync(empire => empire.Id, cancellationToken);
        var primaryOwnerName = ResolveEmpireName(request.PrimaryOwnerEmpireId, request.PrimaryOwnerEmpireName, empiresById);
        var secondaryOwnerName = ResolveEmpireName(request.SecondaryOwnerEmpireId, request.SecondaryOwnerEmpireName, empiresById);
        var resolvedDistance = request.DistanceParsecs > 0
            ? decimal.Round(request.DistanceParsecs, 3)
            : decimal.Round((decimal)SectorRoutePlanner.CalculateParsecDistance(sourceSystem.Coordinates, targetSystem.Coordinates), 3);
        var resolvedTravelTime = request.TravelTimeYears > 0
            ? decimal.Round(request.TravelTimeYears, 6)
            : decimal.Round((decimal)SectorRoutePlanner.CalculateHyperlaneTravelTimeYears(
                sector.Configuration,
                request.TechnologyLevel,
                (double)resolvedDistance), 6);
        var resolvedTierName = string.IsNullOrWhiteSpace(request.TierName)
            ? SectorRoutePlanner.GetTierName(sector.Configuration, request.TechnologyLevel)
            : request.TierName.Trim();

        var route = request.RouteId is int routeId
            ? existingRoutes.FirstOrDefault(item => item.Id == routeId)
                ?? throw new InvalidOperationException("Saved hyperlane was not found.")
            : new SectorSavedRoute
            {
                SectorId = request.SectorId
            };

        route.SourceSystemId = normalizedPair.SourceSystemId;
        route.TargetSystemId = normalizedPair.TargetSystemId;
        route.DistanceParsecs = resolvedDistance;
        route.TravelTimeYears = resolvedTravelTime;
        route.TechnologyLevel = request.TechnologyLevel;
        route.TierName = resolvedTierName;
        route.PrimaryOwnerEmpireId = request.PrimaryOwnerEmpireId;
        route.PrimaryOwnerEmpireName = primaryOwnerName;
        route.SecondaryOwnerEmpireId = request.SecondaryOwnerEmpireId;
        route.SecondaryOwnerEmpireName = secondaryOwnerName;
        route.IsUserPersisted = request.IsUserPersisted;
        route.GeneratedAtUtc = DateTime.UtcNow;

        if (request.RouteId is null)
        {
            dbContext.SectorSavedRoutes.Add(route);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return route;
    }

    public async Task DeleteSavedRouteAsync(
        int sectorId,
        int routeId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var route = await dbContext.SectorSavedRoutes
            .FirstOrDefaultAsync(item => item.SectorId == sectorId && item.Id == routeId, cancellationToken)
            ?? throw new InvalidOperationException("Saved hyperlane was not found.");

        dbContext.SectorSavedRoutes.Remove(route);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SectorSavedRoute CreateSavedRoute(
        int sectorId,
        SectorHyperlaneRouteDefinition route,
        DateTime generatedAtUtc)
    {
        var normalizedPair = NormalizeRoutePair(route.SourceSystemId, route.TargetSystemId);
        return new SectorSavedRoute
        {
            SectorId = sectorId,
            SourceSystemId = normalizedPair.SourceSystemId,
            TargetSystemId = normalizedPair.TargetSystemId,
            DistanceParsecs = decimal.Round((decimal)route.DistanceParsecs, 3),
            TravelTimeYears = decimal.Round((decimal)route.TravelTimeYears, 6),
            TechnologyLevel = (byte)route.TechnologyLevel,
            TierName = route.TierName,
            PrimaryOwnerEmpireId = route.PrimaryOwnerEmpireId,
            PrimaryOwnerEmpireName = route.PrimaryOwnerEmpireName,
            SecondaryOwnerEmpireId = route.SecondaryOwnerEmpireId,
            SecondaryOwnerEmpireName = route.SecondaryOwnerEmpireName,
            GeneratedAtUtc = generatedAtUtc
        };
    }

    private static SectorHyperlaneRouteDefinition MapSavedRouteDefinition(SectorSavedRoute route)
    {
        return new SectorHyperlaneRouteDefinition(
            route.SourceSystemId,
            route.TargetSystemId,
            (double)route.DistanceParsecs,
            (double)route.TravelTimeYears,
            route.TechnologyLevel,
            route.TierName,
            route.PrimaryOwnerEmpireId,
            route.PrimaryOwnerEmpireName,
            route.SecondaryOwnerEmpireId,
            route.SecondaryOwnerEmpireName);
    }

    private static (int SourceSystemId, int TargetSystemId) NormalizeRoutePair(int sourceSystemId, int targetSystemId)
    {
        return sourceSystemId <= targetSystemId
            ? (sourceSystemId, targetSystemId)
            : (targetSystemId, sourceSystemId);
    }

    private static string ResolveEmpireName(
        int? empireId,
        string fallbackName,
        IReadOnlyDictionary<int, Empire> empiresById)
    {
        if (empireId is int resolvedEmpireId
            && empiresById.TryGetValue(resolvedEmpireId, out var empire))
        {
            return empire.Name;
        }

        return fallbackName?.Trim() ?? string.Empty;
    }
}
