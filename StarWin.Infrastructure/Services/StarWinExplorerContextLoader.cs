using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
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
            .Include(empire => empire.Religions)
            .OrderBy(empire => empire.Name)
            .ToListAsync(cancellationToken);

        return new StarWinExplorerContext(
            sectors,
            sectors.FirstOrDefault() ?? StarWinExplorerContext.Empty.CurrentSector,
            alienRaces,
            empires);
    }

    public static async Task<StarWinExplorerContext> LoadShellAsync(
        StarWinDbContext dbContext,
        int? preferredSectorId = null,
        bool includeReferenceData = false,
        CancellationToken cancellationToken = default)
    {
        var sectorRows = await dbContext.Sectors
            .AsNoTracking()
            .OrderBy(sector => sector.Name)
            .Select(sector => new
            {
                sector.Id,
                sector.Name
            })
            .ToListAsync(cancellationToken);

        var systemRows = await dbContext.StarSystems
            .AsNoTracking()
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .Select(system => new
            {
                system.Id,
                system.SectorId,
                system.Name
            })
            .ToListAsync(cancellationToken);

        var sectors = sectorRows
            .Select(row => new StarWinSector
            {
                Id = row.Id,
                Name = row.Name
            })
            .ToList();

        var sectorsById = sectors.ToDictionary(sector => sector.Id);
        foreach (var systemRow in systemRows)
        {
            if (!sectorsById.TryGetValue(systemRow.SectorId, out var sector))
            {
                continue;
            }

            sector.Systems.Add(new StarSystem
            {
                Id = systemRow.Id,
                SectorId = systemRow.SectorId,
                Name = systemRow.Name
            });
        }

        var alienRaces = includeReferenceData
            ? await dbContext.AlienRaces
                .AsNoTracking()
                .OrderBy(race => race.Name)
                .Select(race => new AlienRace
                {
                    Id = race.Id,
                    Name = race.Name
                })
                .ToListAsync(cancellationToken)
            : [];

        var empires = includeReferenceData
            ? await dbContext.Empires
                .AsNoTracking()
                .OrderBy(empire => empire.Name)
                .Select(empire => new Empire
                {
                    Id = empire.Id,
                    Name = empire.Name
                })
                .ToListAsync(cancellationToken)
            : [];

        var currentSector = preferredSectorId is int requestedSectorId && requestedSectorId > 0
            ? sectors.FirstOrDefault(sector => sector.Id == requestedSectorId)
            : null;

        return new StarWinExplorerContext(
            sectors,
            currentSector ?? sectors.FirstOrDefault() ?? StarWinExplorerContext.Empty.CurrentSector,
            alienRaces,
            empires);
    }
}
