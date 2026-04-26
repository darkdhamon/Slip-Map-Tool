using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;

namespace StarWin.Application.Tests.Services;

public sealed class StarWinSearchServiceTests
{
    [Fact]
    public void Search_ReturnsEmptyForBlankQuery()
    {
        var service = new StarWinSearchService(new FakeWorkspace());

        var results = service.Search(" ");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_FindsSystemByLegacyImportId()
    {
        var service = new StarWinSearchService(BuildWorkspace());

        var results = service.Search("SID 42");

        var match = Assert.Single(results);
        Assert.Equal(StarWinSearchResultType.StarSystem, match.Type);
        Assert.Equal("Lacaille", match.Title);
        Assert.Equal("Systems", match.Tab);
    }

    [Fact]
    public void Search_PrioritizesExactTitleMatches()
    {
        var service = new StarWinSearchService(BuildWorkspace());

        var results = service.Search("Aster");

        Assert.NotEmpty(results);
        Assert.Equal("Aster", results[0].Title);
        Assert.Equal(StarWinSearchResultType.AlienRace, results[0].Type);
    }

    [Fact]
    public void Search_RespectsMaxResults()
    {
        var service = new StarWinSearchService(BuildWorkspaceWithManyWorlds());

        var results = service.Search("World", maxResults: 5);

        Assert.Equal(5, results.Count);
    }

    private static FakeWorkspace BuildWorkspace()
    {
        var sector = new StarWinSector { Id = 1, Name = "Delcora Sector" };
        var system = new StarSystem
        {
            Id = 1001,
            LegacySystemId = 42,
            Name = "Lacaille",
            Coordinates = new Coordinates(10, 20, 30)
        };

        var world = new World
        {
            Id = 2001,
            LegacyPlanetId = 7,
            Name = "Cinder",
            Kind = WorldKind.Planet,
            WorldType = "Desert",
            AtmosphereType = "Thin",
            Colony = new Colony
            {
                Id = 3001,
                ColonyClass = "Trade Hub",
                ColonistRaceName = "Aster",
                AllegianceName = "Aster League",
                EstimatedPopulation = 12_000_000,
                RaceId = 9,
                ControllingEmpireId = 14
            }
        };

        system.Worlds.Add(world);
        system.SpaceHabitats.Add(new SpaceHabitat
        {
            Id = 4001,
            Name = "Cinder Halo",
            Population = 2_000_000,
            ControlledByEmpireId = 14
        });
        sector.Systems.Add(system);
        sector.History.Add(new HistoryEvent
        {
            Century = 3,
            EventType = "Battle",
            Description = "A decisive frontier battle.",
            StarSystemId = system.Id,
            PlanetId = world.Id,
            RaceId = 9
        });

        return new FakeWorkspace
        {
            Sectors = [sector],
            CurrentSector = sector,
            AlienRaces =
            [
                new AlienRace
                {
                    Id = 9,
                    Name = "Aster",
                    AppearanceType = "Avian",
                    EnvironmentType = "Temperate",
                    BodyChemistry = "Carbon"
                }
            ],
            Empires =
            [
                new Empire
                {
                    Id = 14,
                    Name = "Aster League",
                    Planets = 3,
                    MilitaryPower = 44,
                    CivilizationProfile = new CivilizationProfile { TechLevel = 11 },
                    LegacyRaceId = 9
                }
            ]
        };
    }

    private static FakeWorkspace BuildWorkspaceWithManyWorlds()
    {
        var workspace = BuildWorkspace();
        var system = workspace.Sectors[0].Systems[0];
        for (var index = 0; index < 10; index++)
        {
            system.Worlds.Add(new World
            {
                Id = 5000 + index,
                Name = $"World {index}",
                Kind = WorldKind.Planet,
                WorldType = "Rocky",
                AtmosphereType = "Standard"
            });
        }

        return workspace;
    }

    private sealed class FakeWorkspace : IStarWinWorkspace
    {
        public bool IsLoaded => true;

        public IReadOnlyList<StarWinSector> Sectors { get; init; } = [];

        public StarWinSector CurrentSector { get; init; } = new();

        public IReadOnlyList<AlienRace> AlienRaces { get; init; } = [];

        public IReadOnlyList<Empire> Empires { get; init; } = [];

        public IReadOnlyList<EmpireContact> EmpireContacts { get; init; } = [];

        public CivilizationGeneratorSettings CivilizationSettings { get; } = new();

        public ArmyGeneratorSettings ArmySettings { get; } = new();

        public GurpsTemplate PreviewGurpsTemplate { get; } = new();

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
