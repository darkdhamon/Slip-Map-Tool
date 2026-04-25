using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class EmpiresPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedEmpireInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Empire profile", cut.Markup);
            Assert.Contains("Orion Compact", cut.Markup);
            Assert.Contains("Homeworld: Helios", cut.Markup);
            Assert.Contains("href=\"/sector-explorer/worlds?sectorId=7\"", cut.Markup);
        });
    }

    [Fact]
    public void FiltersEmpiresBySearchQueryAndClearsFilters()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            primaryEmpireName: "Orion Compact",
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Zephyr League",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 2,
                    nativePopulationMillions: 800)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Zephyria",
                    colonyId: 202,
                    colonyName: "Zephyria Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]));

        var cut = Render<Empires>();

        cut.Find("input[placeholder='Name, policy, homeworld...']").Input("Zephyr");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Zephyr League", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 empire", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Clear filters")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".record-row").Count);
            Assert.Contains("Showing 2 empires", cut.Markup);
        });
    }

    [Fact]
    public void FiltersEmpiresByRaceWhenRaceFilterIsApplied()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalRaces:
            [
                new AlienRace
                {
                    Id = 2,
                    Name = "Krell"
                }
            ],
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Krell Dominion",
                    foundingWorldId: 102,
                    primaryRaceId: 2,
                    planets: 5,
                    nativePopulationMillions: 2100)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Kharon",
                    colonyId: 202,
                    colonyName: "Kharon Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]));

        var cut = Render<Empires>();

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Show filters")
            .Click();

        cut.Find("input[placeholder='All races']").Input("2 - Krell");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Krell Dominion", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 empire", cut.Markup);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalEmpires()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var additionalEmpires = new List<Empire>();
        var additionalWorlds = new List<World>();

        for (var index = 0; index < 121; index++)
        {
            var empireId = index + 10;
            var worldId = 200 + index;
            additionalEmpires.Add(CreateEmpire(
                empireId,
                $"Empire {index:D3}",
                foundingWorldId: worldId,
                primaryRaceId: 1,
                planets: 1,
                nativePopulationMillions: 10));
            additionalWorlds.Add(CreateWorld(
                worldId,
                $"World {index:D3}",
                colonyId: 500 + index,
                colonyName: $"Colony {index:D3}",
                controllingEmpireId: empireId,
                foundingEmpireId: empireId));
        }

        ConfigureServices(CreateContext(
            additionalEmpires: additionalEmpires,
            additionalWorlds: additionalWorlds));

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 120 empires+", cut.Markup);
            Assert.DoesNotContain("Empire 120", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Load more")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 122 empires", cut.Markup);
            Assert.Contains("Empire 120", cut.Markup);
        });
    }

    [Fact]
    public void NavigatesToRelatedExplorerPagesFromEmpireActions()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() => Assert.Contains("Homeworld: Helios", cut.Markup));

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Homeworld: Helios")
            .Click();

        Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101&empireId=2", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Capital: Helios")
            .Click();

        Assert.EndsWith("/sector-explorer/colonies?sectorId=7&systemId=11&colonyId=201&empireId=2", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersFallenEmpireStatusWhenEmpireHasNoControlledColonies()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            primaryEmpireName: "Ancient Watchers",
            primaryEmpireNativePopulationMillions: 950,
            primaryEmpirePlanets: 1,
            primaryWorld: CreateWorld(
                101,
                "Helios",
                colonyId: 201,
                colonyName: "Helios Prime",
                controllingEmpireId: 3,
                foundingEmpireId: 2)));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Fallen empire", cut.Markup);
            Assert.Contains("This empire no longer controls any colonies in this sector.", cut.Markup);
            Assert.Contains("<dd>Fallen empire</dd>", cut.Markup);
        });
    }

    private void ConfigureServices(StarWinExplorerContext context)
    {
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());
    }

    private static StarWinExplorerContext CreateContext(
        string primaryEmpireName = "Orion Compact",
        int primaryEmpirePlanets = 4,
        long primaryEmpireNativePopulationMillions = 3200,
        World? primaryWorld = null,
        IEnumerable<AlienRace>? additionalRaces = null,
        IEnumerable<Empire>? additionalEmpires = null,
        IEnumerable<World>? additionalWorlds = null)
    {
        var world = primaryWorld ?? CreateWorld(
            101,
            "Helios",
            colonyId: 201,
            colonyName: "Helios Prime",
            controllingEmpireId: 2,
            foundingEmpireId: 2);

        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios"
        };
        system.Worlds.Add(world);

        if (additionalWorlds is not null)
        {
            foreach (var additionalWorld in additionalWorlds)
            {
                system.Worlds.Add(additionalWorld);
            }
        }

        var sector = new StarWinSector
        {
            Id = 7,
            Name = "Del Corra"
        };
        sector.Systems.Add(system);

        var race = new AlienRace
        {
            Id = 1,
            Name = "Human"
        };

        var races = new List<AlienRace> { race };
        if (additionalRaces is not null)
        {
            races.AddRange(additionalRaces);
        }

        var empire = CreateEmpire(
            2,
            primaryEmpireName,
            foundingWorldId: world.Id,
            primaryRaceId: 1,
            planets: primaryEmpirePlanets,
            nativePopulationMillions: primaryEmpireNativePopulationMillions);

        var empires = new List<Empire> { empire };
        if (additionalEmpires is not null)
        {
            empires.AddRange(additionalEmpires);
        }

        return new StarWinExplorerContext([sector], sector, races, empires, []);
    }

    private static World CreateWorld(
        int worldId,
        string worldName,
        int colonyId,
        string colonyName,
        int controllingEmpireId,
        int foundingEmpireId)
    {
        var world = new World
        {
            Id = worldId,
            Name = worldName,
            WorldType = "Terran",
            AtmosphereType = "Breathable",
            StarSystemId = 11
        };

        world.Colony = new Colony
        {
            Id = colonyId,
            WorldId = world.Id,
            Name = colonyName,
            ColonyClass = "Capital",
            EstimatedPopulation = 3_200_000_000,
            RaceId = 1,
            ColonistRaceName = "Human",
            ControllingEmpireId = controllingEmpireId,
            FoundingEmpireId = foundingEmpireId,
            AllegianceId = (ushort)controllingEmpireId,
            AllegianceName = controllingEmpireId == 2 ? "Orion Compact" : $"Empire {controllingEmpireId}"
        };

        return world;
    }

    private static Empire CreateEmpire(
        int empireId,
        string name,
        int foundingWorldId,
        int primaryRaceId,
        int planets,
        long nativePopulationMillions)
    {
        var empire = new Empire
        {
            Id = empireId,
            Name = name,
            Planets = planets,
            MilitaryPower = 9,
            NativePopulationMillions = nativePopulationMillions
        };
        empire.CivilizationProfile.TechLevel = 9;
        empire.Founding.FoundingWorldId = foundingWorldId;
        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = primaryRaceId,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = nativePopulationMillions
        });

        return empire;
    }

    private sealed class FakeExplorerContextService(StarWinExplorerContext context) : IStarWinExplorerContextService
    {
        public Task<StarWinExplorerContext> LoadShellAsync(bool includeSavedRoutes = true, bool includeReferenceData = true, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context);
        }

        public Task<StarWinSector?> LoadSectorAsync(int sectorId, ExplorerSectorLoadSections loadSections, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StarWinSector?>(context.Sectors.FirstOrDefault(sector => sector.Id == sectorId));
        }
    }

    private sealed class FakeSearchService : IStarWinSearchService
    {
        public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30)
        {
            return [];
        }
    }

    private sealed class FakeImageService : IStarWinImageService
    {
        public Task<IReadOnlyList<EntityImage>> GetImagesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<EntityImage>>([]);
        }

        public Task<EntityImage> UploadImageAsync(EntityImageTargetKind targetKind, int targetId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EntityImage
            {
                TargetKind = targetKind,
                TargetId = targetId,
                FileName = fileName
            });
        }
    }

    private sealed class FakeEntityNameService : IStarWinEntityNameService
    {
        public Task<string> SaveNameAsync(EntityNoteTargetKind targetKind, int targetId, string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(name);
        }
    }

    private sealed class FakeEntityNoteService : IStarWinEntityNoteService
    {
        public Task<EntityNote?> GetNoteAsync(EntityNoteTargetKind targetKind, int targetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<EntityNote?>(null);
        }

        public Task<EntityNote?> SaveNoteAsync(EntityNoteTargetKind targetKind, int targetId, string markdown, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<EntityNote?>(new EntityNote
            {
                TargetKind = targetKind,
                TargetId = targetId,
                Markdown = markdown
            });
        }
    }
}
