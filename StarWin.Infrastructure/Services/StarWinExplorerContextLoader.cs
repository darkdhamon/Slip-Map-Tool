using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
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
}
