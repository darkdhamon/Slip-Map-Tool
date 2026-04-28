using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class IndependentColonyEmpireFactoryTests
{
    [Fact]
    public void CreateEmpireFromIndependentColony_SeedsEmpireFromWorldAndRace()
    {
        var factory = new IndependentColonyEmpireFactory(() => 0.10);
        var colony = new Colony
        {
            Id = 8,
            GrossWorldProductMcr = 900,
            MilitaryPower = 17,
            EstimatedPopulation = 23_000_000
        };
        var world = new World { Id = 4, Name = "Delcora Prime" };
        var race = new AlienRace
        {
            Id = 22,
            Name = "Nemorans",
            CivilizationProfile =
            {
                Militancy = 8,
                Determination = 9,
                RacialTolerance = 7,
                Progressiveness = 6,
                Loyalty = 10,
                SocialCohesion = 11,
                Art = 12,
                Individualism = 13
            }
        };
        var parentEmpire = new Empire
        {
            Id = 51,
            CivilizationProfile =
            {
                TechLevel = 14,
                SpatialAge = 15
            }
        };

        var empire = factory.CreateEmpireFromIndependentColony(colony, world, race, parentEmpire);

        Assert.Equal("Nemorans Independent State", empire.Name);
        Assert.Equal(22, empire.LegacyRaceId);
        Assert.Equal(EmpireOrigin.IndependentColony, empire.Founding.Origin);
        Assert.Equal(4, empire.Founding.FoundingWorldId);
        Assert.Equal(8, empire.Founding.FoundingColonyId);
        Assert.Equal(22, empire.Founding.FoundingRaceId);
        Assert.Equal(51, empire.Founding.ParentEmpireId);
        Assert.Equal(900, empire.EconomicPowerMcr);
        Assert.Equal(17, empire.MilitaryPower);
        Assert.Equal(23, empire.IndependentPopulationMillions);
        Assert.Equal(0, empire.CivilizationModifiers.Militancy);
        Assert.Equal(8, empire.CivilizationProfile.Militancy);
        Assert.Equal(14, empire.CivilizationProfile.TechLevel);
        Assert.Equal(15, empire.CivilizationProfile.SpatialAge);
        Assert.Single(empire.RaceMemberships);
        Assert.True(empire.RaceMemberships[0].IsPrimary);
        Assert.Equal(EmpireRaceRole.Founder, empire.RaceMemberships[0].Role);
    }

    [Fact]
    public void CreateEmpireFromIndependentColony_AssignsWeightedCivilizationModifiers()
    {
        var rolls = new Queue<double>([0.60, 0.70, 0.80, 0.88, 0.93, 0.97, 0.10, 0.62]);
        var factory = new IndependentColonyEmpireFactory(() => rolls.Dequeue());
        var colony = new Colony
        {
            Id = 8,
            GrossWorldProductMcr = 900,
            MilitaryPower = 17,
            EstimatedPopulation = 23_000_000
        };
        var world = new World { Id = 4, Name = "Delcora Prime" };
        var race = new AlienRace
        {
            Id = 22,
            Name = "Nemorans",
            CivilizationProfile =
            {
                Militancy = 8,
                Determination = 9,
                RacialTolerance = 7,
                Progressiveness = 6,
                Loyalty = 10,
                SocialCohesion = 11,
                Art = 12,
                Individualism = 13
            }
        };

        var empire = factory.CreateEmpireFromIndependentColony(colony, world, race, parentEmpire: null);

        Assert.Equal(1, empire.CivilizationModifiers.Militancy);
        Assert.Equal(-1, empire.CivilizationModifiers.Determination);
        Assert.Equal(2, empire.CivilizationModifiers.RacialTolerance);
        Assert.Equal(-2, empire.CivilizationModifiers.Progressiveness);
        Assert.Equal(3, empire.CivilizationModifiers.Loyalty);
        Assert.Equal(-3, empire.CivilizationModifiers.SocialCohesion);
        Assert.Equal(0, empire.CivilizationModifiers.Art);
        Assert.Equal(1, empire.CivilizationModifiers.Individualism);

        Assert.Equal(9, empire.CivilizationProfile.Militancy);
        Assert.Equal(8, empire.CivilizationProfile.Determination);
        Assert.Equal(9, empire.CivilizationProfile.RacialTolerance);
        Assert.Equal(4, empire.CivilizationProfile.Progressiveness);
        Assert.Equal(13, empire.CivilizationProfile.Loyalty);
        Assert.Equal(8, empire.CivilizationProfile.SocialCohesion);
        Assert.Equal(12, empire.CivilizationProfile.Art);
        Assert.Equal(14, empire.CivilizationProfile.Individualism);
    }

    [Fact]
    public void CreateEmpireFromIndependentColony_FallsBackToParentEmpireNameWhenRaceNameIsPlaceholder()
    {
        var factory = new IndependentColonyEmpireFactory(() => 0.10);
        var colony = new Colony
        {
            Id = 8,
            EstimatedPopulation = 23_000_000
        };
        var world = new World { Id = 4, Name = "Delcora Prime" };
        var race = new AlienRace
        {
            Id = 22,
            Name = "Race 22"
        };
        var parentEmpire = new Empire
        {
            Id = 51,
            Name = "Nemoran Concord"
        };

        var empire = factory.CreateEmpireFromIndependentColony(colony, world, race, parentEmpire);

        Assert.Equal("Nemoran Concord", empire.Name);
    }
}
