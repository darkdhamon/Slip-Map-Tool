using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

internal static class StarWinExplorerContextLoader
{
    public static async Task<StarWinExplorerContext> LoadAsync(
        StarWinDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var sectors = await dbContext.Sectors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(sector => sector.Configuration)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.AstralBodies)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.UnusualCharacteristics)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.Colony)
                        .ThenInclude(colony => colony!.Demographics)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.SpaceHabitats)
            .Include(sector => sector.SavedRoutes)
            .Include(sector => sector.History)
            .OrderBy(sector => sector.Name)
            .ToListAsync(cancellationToken);

        var alienRaces = await dbContext.AlienRaces
            .AsNoTracking()
            .OrderBy(race => race.Name)
            .ToListAsync(cancellationToken);

        var empires = await dbContext.Empires
            .AsNoTracking()
            .AsSplitQuery()
            .Include(empire => empire.RaceMemberships)
            .Include(empire => empire.Contacts)
            .OrderBy(empire => empire.Name)
            .ToListAsync(cancellationToken);

        return new StarWinExplorerContext(
            sectors,
            sectors.FirstOrDefault() ?? StarWinExplorerContext.Empty.CurrentSector,
            alienRaces,
            empires,
            empires.SelectMany(empire => empire.Contacts).ToList());
    }

    public static async Task<StarWinExplorerContext> LoadShellAsync(
        StarWinDbContext dbContext,
        bool includeSavedRoutes = true,
        bool includeReferenceData = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StarWinSector> sectorsQuery = dbContext.Sectors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(sector => sector.Configuration)
            .Include(sector => sector.Systems)
            .OrderBy(sector => sector.Name);

        if (includeSavedRoutes)
        {
            sectorsQuery = sectorsQuery.Include(sector => sector.SavedRoutes);
        }

        var sectors = await sectorsQuery.ToListAsync(cancellationToken);

        var alienRaces = includeReferenceData
            ? await dbContext.AlienRaces
                .AsNoTracking()
                .OrderBy(race => race.Name)
                .ToListAsync(cancellationToken)
            : [];

        var empires = includeReferenceData
            ? await dbContext.Empires
                .AsNoTracking()
                .AsSplitQuery()
                .Include(empire => empire.RaceMemberships)
                .Include(empire => empire.Contacts)
                .OrderBy(empire => empire.Name)
                .ToListAsync(cancellationToken)
            : [];

        return new StarWinExplorerContext(
            sectors,
            sectors.FirstOrDefault() ?? StarWinExplorerContext.Empty.CurrentSector,
            alienRaces,
            empires,
            includeReferenceData ? empires.SelectMany(empire => empire.Contacts).ToList() : []);
    }

    public static async Task<StarWinSector?> LoadSectorAsync(
        StarWinDbContext dbContext,
        int sectorId,
        bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StarWinSector> sectorQuery = dbContext.Sectors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(sector => sector.Configuration)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.AstralBodies)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.UnusualCharacteristics)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.Worlds)
                    .ThenInclude(world => world.Colony)
                        .ThenInclude(colony => colony!.Demographics)
            .Include(sector => sector.Systems)
                .ThenInclude(system => system.SpaceHabitats)
            .Include(sector => sector.SavedRoutes);

        if (includeHistory)
        {
            sectorQuery = sectorQuery.Include(sector => sector.History);
        }

        return await sectorQuery.FirstOrDefaultAsync(sector => sector.Id == sectorId, cancellationToken);
    }
}
