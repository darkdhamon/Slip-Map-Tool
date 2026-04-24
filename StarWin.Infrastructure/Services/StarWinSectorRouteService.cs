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
        CancellationToken cancellationToken = default)
    {
        var sector = await dbContext.Sectors
            .AsSplitQuery()
            .Include(item => item.Configuration)
            .Include(item => item.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.Colony)
            .FirstOrDefaultAsync(item => item.Id == sectorId, cancellationToken)
            ?? throw new InvalidOperationException("Sector was not found.");

        var empiresById = await dbContext.Empires
            .AsNoTracking()
            .ToDictionaryAsync(empire => empire.Id, cancellationToken);

        var routes = SectorRoutePlanner.BuildHyperlaneRoutes(sector, empiresById);
        var generatedAtUtc = DateTime.UtcNow;
        var existingRoutes = await dbContext.SectorSavedRoutes
            .Where(route => route.SectorId == sectorId)
            .ToListAsync(cancellationToken);

        var replacedExistingRoutes = existingRoutes.Count > 0;
        if (existingRoutes.Count > 0)
        {
            dbContext.SectorSavedRoutes.RemoveRange(existingRoutes);
        }

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
        return new SectorRouteSaveResult(sectorId, routes.Count, replacedExistingRoutes, generatedAtUtc);
    }
}
