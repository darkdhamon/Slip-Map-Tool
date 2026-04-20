using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinIndependentColonyService(StarWinDbContext dbContext) : IStarWinIndependentColonyService
{
    private readonly IndependentColonyEmpireFactory independentColonyEmpireFactory = new();

    public async Task<IReadOnlyList<Empire>> ConvertIndependentColoniesAsync(
        int sectorId,
        CancellationToken cancellationToken = default)
    {
        var existingFoundingColonyIds = await dbContext.Empires
            .Where(empire => empire.Founding.Origin == EmpireOrigin.IndependentColony
                && empire.Founding.FoundingColonyId != null)
            .Select(empire => empire.Founding.FoundingColonyId!.Value)
            .ToHashSetAsync(cancellationToken);
        var racesById = await dbContext.AlienRaces
            .ToDictionaryAsync(race => race.Id, cancellationToken);
        var empiresById = await dbContext.Empires
            .ToDictionaryAsync(empire => empire.Id, cancellationToken);
        var worlds = await dbContext.Worlds
            .Include(world => world.Colony)
                .ThenInclude(colony => colony!.Demographics)
            .Where(world => world.StarSystemId != null
                && dbContext.StarSystems.Any(system => system.Id == world.StarSystemId && system.SectorId == sectorId))
            .ToListAsync(cancellationToken);

        var nextEmpireId = (empiresById.Count == 0 ? 0 : empiresById.Keys.Max()) + 1;
        var createdEmpires = new List<Empire>();
        foreach (var world in worlds)
        {
            if (world.Colony is not { } colony
                || !IsIndependent(colony)
                || !existingFoundingColonyIds.Add(colony.Id)
                || !racesById.TryGetValue(colony.RaceId, out var foundingRace))
            {
                continue;
            }

            var parentEmpire = ResolveEmpire(empiresById, colony.ParentEmpireId ?? colony.FoundingEmpireId);
            var empire = independentColonyEmpireFactory.CreateEmpireFromIndependentColony(
                colony,
                world,
                foundingRace,
                parentEmpire);
            while (empiresById.ContainsKey(nextEmpireId))
            {
                nextEmpireId++;
            }

            empire.Id = nextEmpireId++;
            colony.ControllingEmpireId = empire.Id;
            dbContext.Empires.Add(empire);
            empiresById[empire.Id] = empire;
            createdEmpires.Add(empire);
        }

        if (createdEmpires.Count > 0)
        {
            dbContext.HistoryEvents.Add(new HistoryEvent
            {
                SectorId = sectorId,
                Century = 0,
                EventType = "Configuration",
                Description = $"Converted {createdEmpires.Count:N0} independent colonies into empires."
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return createdEmpires;
    }

    private static bool IsIndependent(Colony colony)
    {
        return colony.PoliticalStatus == ColonyPoliticalStatus.Independent
            || colony.AllegianceId == ushort.MaxValue;
    }

    private static Empire? ResolveEmpire(IReadOnlyDictionary<int, Empire> empiresById, int? empireId)
    {
        return empireId is { } id && empiresById.TryGetValue(id, out var empire)
            ? empire
            : null;
    }
}
