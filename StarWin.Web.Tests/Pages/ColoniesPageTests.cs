using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Layout;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class ColoniesPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedColonyInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/colonies?sectorId=7&colonyId=201");

        var cut = Render<Colonies>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Colony record", cut.Markup);
            Assert.Contains("Helios Prime", cut.Markup);
            Assert.Contains("Parent world: Helios", cut.Markup);
        });
    }

    [Fact]
    public void FiltersColoniesBySearchQueryAndClearsFilters()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext(additionalWorlds: [CreateWorldWithColony(102, "Zephyria", 202, "Zephyria Prime", 3, "Zephyr League", 2_400_000_000)]));

        var cut = Render<Colonies>();
        cut.Find("input[placeholder='Name, allegiance, population...']").Input("Zephyr");

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".record-row");
            Assert.Single(rows);
            Assert.Contains("Zephyria Prime", rows[0].TextContent);
            Assert.Contains("Showing 1 colony", cut.Markup);
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear filters").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".record-row").Count);
            Assert.Contains("Showing 2 colonies", cut.Markup);
        });
    }

    [Fact]
    public void FiltersColoniesByRaceWhenFilterIsApplied()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext(
            additionalRaces: [new AlienRace { Id = 2, Name = "Krell" }],
            additionalWorlds: [CreateWorldWithColony(102, "Kharon", 202, "Kharon Prime", 2, "Orion Compact", 2_400_000_000, 2, "Krell")]));

        var cut = Render<Colonies>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Show filters").Click();
        cut.Find("input[placeholder='All races']").Input("2 - Krell");

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".record-row");
            Assert.Single(rows);
            Assert.Contains("Kharon Prime", rows[0].TextContent);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalColonies()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var worlds = new List<World>();
        for (var index = 0; index < 121; index++)
        {
            worlds.Add(CreateWorldWithColony(200 + index, $"World {index:D3}", 500 + index, $"Colony {index:D3}", 2, "Orion Compact", 10_000_000 + index));
        }

        ConfigureServices(CreateContext(additionalWorlds: worlds));
        var cut = Render<Colonies>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 120 colonies+", cut.Markup);
            Assert.DoesNotContain("Colony 000", cut.Markup);
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Load more").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 122 colonies", cut.Markup);
            Assert.Contains("Colony 000", cut.Markup);
        });
    }

    [Fact]
    public void NavigatesToRelatedExplorerPagesFromColonyActions()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/colonies?sectorId=7&colonyId=201");

        var cut = Render<Colonies>();
        cut.WaitForAssertion(() => Assert.Contains("Parent world: Helios", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Parent world: Helios").Click();
        Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101&colonyId=201", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll(".demographic-row").Single(button => button.TextContent.Contains("Human", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/aliens?sectorId=7&systemId=11&colonyId=201&raceId=1", navigationManager.Uri, StringComparison.Ordinal);
    }

    private void ConfigureServices(StarWinExplorerContext context)
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());
    }

    private static StarWinExplorerContext CreateContext(
        IEnumerable<AlienRace>? additionalRaces = null,
        IEnumerable<World>? additionalWorlds = null)
    {
        var primaryWorld = CreateWorldWithColony(101, "Helios", 201, "Helios Prime", 2, "Orion Compact", 3_200_000_000);
        primaryWorld.Colony!.Demographics.Add(new ColonyDemographic { RaceId = 1, RaceName = "Human", PopulationPercent = 92 });

        var system = new StarSystem { Id = 11, SectorId = 7, Name = "Helios" };
        system.Worlds.Add(primaryWorld);
        if (additionalWorlds is not null)
        {
            foreach (var world in additionalWorlds)
            {
                if (world.Colony?.Demographics.Count == 0)
                {
                    world.Colony?.Demographics.Add(new ColonyDemographic
                    {
                        RaceId = world.Colony?.RaceId ?? 1,
                        RaceName = world.Colony?.ColonistRaceName ?? "Human",
                        PopulationPercent = 90
                    });
                }
                system.Worlds.Add(world);
            }
        }

        var sector = new StarWinSector { Id = 7, Name = "Del Corra" };
        sector.Systems.Add(system);

        var races = new List<AlienRace>
        {
            new AlienRace { Id = 1, Name = "Human" }
        };
        if (additionalRaces is not null)
        {
            races.AddRange(additionalRaces);
        }

        var empire = new Empire { Id = 2, Name = "Orion Compact" };
        var extraEmpire = new Empire { Id = 3, Name = "Zephyr League" };

        return new StarWinExplorerContext([sector], sector, races, [empire, extraEmpire], []);
    }

    private static World CreateWorldWithColony(int worldId, string worldName, int colonyId, string colonyName, int allegianceId, string allegianceName, long population, int raceId = 1, string raceName = "Human")
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
            EstimatedPopulation = population,
            RaceId = (ushort)raceId,
            ColonistRaceName = raceName,
            AllegianceId = (ushort)allegianceId,
            AllegianceName = allegianceName,
            ControllingEmpireId = (ushort)allegianceId,
            FoundingEmpireId = (ushort)allegianceId,
            PoliticalStatus = ColonyPoliticalStatus.Independent,
            Starport = "A",
            ExportResource = "Water",
            ImportResource = "Machinery"
        };

        return world;
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
        public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30) => [];
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
        public Task<string> SaveNameAsync(EntityNoteTargetKind targetKind, int targetId, string name, CancellationToken cancellationToken = default) => Task.FromResult(name);
    }

    private sealed class FakeEntityNoteService : IStarWinEntityNoteService
    {
        public Task<EntityNote?> GetNoteAsync(EntityNoteTargetKind targetKind, int targetId, CancellationToken cancellationToken = default) => Task.FromResult<EntityNote?>(null);

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
