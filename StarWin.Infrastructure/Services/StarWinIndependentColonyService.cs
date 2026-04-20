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

    public async Task<IndependentColonyConversionResult> ConvertIndependentColoniesAsync(
        int sectorId,
        CancellationToken cancellationToken = default)
    {
        var independentEmpires = await dbContext.Empires
            .Where(empire => empire.Founding.Origin == EmpireOrigin.IndependentColony
                && empire.Founding.FoundingColonyId != null)
            .ToListAsync(cancellationToken);
        var independentEmpiresByFoundingColonyId = independentEmpires
            .GroupBy(empire => empire.Founding.FoundingColonyId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(empire => empire.Id).First());
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
        var assignments = new List<IndependentColonyAssignment>();
        foreach (var world in worlds)
        {
            if (world.Colony is not { } colony
                || !IsIndependent(colony))
            {
                continue;
            }

            if (independentEmpiresByFoundingColonyId.TryGetValue(colony.Id, out var existingEmpire))
            {
                AssignColonyToIndependentEmpire(colony, existingEmpire);
                assignments.Add(new IndependentColonyAssignment(colony.Id, existingEmpire.Id, existingEmpire.Name));
                continue;
            }

            if (!racesById.TryGetValue(colony.RaceId, out var foundingRace))
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
            AssignColonyToIndependentEmpire(colony, empire);
            dbContext.Empires.Add(empire);
            empiresById[empire.Id] = empire;
            independentEmpiresByFoundingColonyId[colony.Id] = empire;
            createdEmpires.Add(empire);
            assignments.Add(new IndependentColonyAssignment(colony.Id, empire.Id, empire.Name));
        }

        if (assignments.Count > 0)
        {
            dbContext.HistoryEvents.Add(new HistoryEvent
            {
                SectorId = sectorId,
                Century = 0,
                EventType = "Configuration",
                Description = createdEmpires.Count == assignments.Count
                    ? $"Converted {createdEmpires.Count:N0} independent colonies into empires."
                    : $"Converted {createdEmpires.Count:N0} independent colonies into empires and assigned {assignments.Count:N0} colonies to independent empires."
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new IndependentColonyConversionResult(createdEmpires, assignments);
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

    private static void AssignColonyToIndependentEmpire(Colony colony, Empire empire)
    {
        colony.ControllingEmpireId = empire.Id;
        colony.FoundingEmpireId = empire.Id;
        colony.PoliticalStatus = ColonyPoliticalStatus.Controlled;
        colony.AllegianceName = empire.Name;
    }
}
