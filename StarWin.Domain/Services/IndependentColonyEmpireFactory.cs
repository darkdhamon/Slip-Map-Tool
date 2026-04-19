using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class IndependentColonyEmpireFactory : IIndependentColonyEmpireFactory
{
    public Empire CreateEmpireFromIndependentColony(Colony colony, World world, AlienRace foundingRace, Empire? parentEmpire)
    {
        ArgumentNullException.ThrowIfNull(colony);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(foundingRace);

        var empire = new Empire
        {
            Name = string.IsNullOrWhiteSpace(world.Name)
                ? $"{foundingRace.Name} Independent State"
                : $"{world.Name} Independent State",
            LegacyRaceId = foundingRace.Id,
            ExpansionPolicy = EmpireExpansionPolicy.CanExpand,
            Founding =
            {
                Origin = EmpireOrigin.IndependentColony,
                FoundingWorldId = world.Id,
                FoundingColonyId = colony.Id,
                FoundingRaceId = foundingRace.Id,
                ParentEmpireId = parentEmpire?.Id
            },
            EconomicPowerMcr = colony.GrossWorldProductMcr,
            MilitaryPower = colony.MilitaryPower,
            IndependentPopulationMillions = colony.EstimatedPopulation / 1_000_000
        };

        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = foundingRace.Id,
            Role = EmpireRaceRole.Founder,
            PopulationMillions = colony.EstimatedPopulation / 1_000_000,
            IsPrimary = true
        });

        return empire;
    }
}
