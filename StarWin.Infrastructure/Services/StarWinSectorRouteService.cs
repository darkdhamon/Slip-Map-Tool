using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinSectorRouteService(StarWinDbContext dbContext) : IStarWinSectorRouteService
{
    public async Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(
        int sectorId,
        IProgress<SectorRouteSaveProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
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

        progress?.Report(new SectorRouteSaveProgress("Generating routes", $"Evaluating route eligibility across {sector.Systems.Count:N0} systems.", 55));
        await Task.Yield();

        var routes = await Task.Run(
            () => SectorRoutePlanner.BuildHyperlaneRoutes(sector, empiresById),
            cancellationToken);
        var generatedAtUtc = DateTime.UtcNow;

        progress?.Report(new SectorRouteSaveProgress("Preparing saved routes", $"Merging {routes.Count:N0} route segment{(routes.Count == 1 ? string.Empty : "s")} into the sector cache.", 75));
        await Task.Yield();

        var existingRoutes = await dbContext.SectorSavedRoutes
            .Where(route => route.SectorId == sectorId)
            .ToListAsync(cancellationToken);

        var replacedExistingRoutes = existingRoutes.Count > 0;
        if (existingRoutes.Count > 0)
        {
            dbContext.SectorSavedRoutes.RemoveRange(existingRoutes);
        }

        progress?.Report(new SectorRouteSaveProgress("Writing database", "Saving the updated route cache to the database.", 90));
        await Task.Yield();

        dbContext.SectorSavedRoutes.AddRange(routes.Select(route => new SectorSavedRoute
        {
            SectorId = sectorId,
            SourceSystemId = route.SourceSystemId,
            TargetSystemId = route.TargetSystemId,
            DistanceParsecs = decimal.Round((decimal)route.DistanceParsecs, 3),
            TravelTimeYears = decimal.Round((decimal)route.TravelTimeYears, 6),
            TechnologyLevel = (byte)route.TechnologyLevel,
            TierName = route.TierName,
            PrimaryOwnerEmpireId = route.PrimaryOwnerEmpireId,
            PrimaryOwnerEmpireName = route.PrimaryOwnerEmpireName,
            SecondaryOwnerEmpireId = route.SecondaryOwnerEmpireId,
            SecondaryOwnerEmpireName = route.SecondaryOwnerEmpireName,
            GeneratedAtUtc = generatedAtUtc
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
        progress?.Report(new SectorRouteSaveProgress("Routes saved", $"Stored {routes.Count:N0} cached route segment{(routes.Count == 1 ? string.Empty : "s")} for this sector.", 100));
        return new SectorRouteSaveResult(sectorId, routes.Count, replacedExistingRoutes, generatedAtUtc);
    }
}
