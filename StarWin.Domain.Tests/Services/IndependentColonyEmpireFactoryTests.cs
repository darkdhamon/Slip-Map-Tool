using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class IndependentColonyEmpireFactoryTests
{
    [Fact]
    public void CreateEmpireFromIndependentColony_SeedsEmpireFromWorldAndRace()
    {
        var factory = new IndependentColonyEmpireFactory();
        var colony = new Colony
        {
            Id = 8,
            GrossWorldProductMcr = 900,
            MilitaryPower = 17,
            EstimatedPopulation = 23_000_000
        };
        var world = new World { Id = 4, Name = "Delcora Prime" };
        var race = new AlienRace { Id = 22, Name = "Nemorans" };
        var parentEmpire = new Empire { Id = 51 };

        var empire = factory.CreateEmpireFromIndependentColony(colony, world, race, parentEmpire);

        Assert.Equal("Delcora Prime Independent State", empire.Name);
        Assert.Equal(22, empire.LegacyRaceId);
        Assert.Equal(EmpireOrigin.IndependentColony, empire.Founding.Origin);
        Assert.Equal(4, empire.Founding.FoundingWorldId);
        Assert.Equal(8, empire.Founding.FoundingColonyId);
        Assert.Equal(22, empire.Founding.FoundingRaceId);
        Assert.Equal(51, empire.Founding.ParentEmpireId);
        Assert.Equal(900, empire.EconomicPowerMcr);
        Assert.Equal(17, empire.MilitaryPower);
        Assert.Equal(23, empire.IndependentPopulationMillions);
        Assert.Single(empire.RaceMemberships);
        Assert.True(empire.RaceMemberships[0].IsPrimary);
        Assert.Equal(EmpireRaceRole.Founder, empire.RaceMemberships[0].Role);
    }
}
