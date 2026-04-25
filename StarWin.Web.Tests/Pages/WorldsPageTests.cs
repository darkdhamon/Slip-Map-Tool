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

public sealed class WorldsPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedWorldInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101");

        var cut = Render<Worlds>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Planet survey", cut.Markup);
            Assert.Contains("Eos", cut.Markup);
            Assert.Contains("Atmosphere", cut.Markup);
        });
    }

    [Fact]
    public void RendersRequestedHabitatInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/worlds?sectorId=7&systemId=11&habitatId=201");

        var cut = Render<Worlds>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Space habitat", cut.Markup);
            Assert.Contains("Arc Habitat", cut.Markup);
            Assert.Contains("Orbit target", cut.Markup);
        });
    }

    [Fact]
    public void FiltersWorldsBySearchQueryAndClearsFilters()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext(additionalWorlds: [CreateWorld(103, "Zephyria", "Oceanic")]));

        var cut = Render<Worlds>();
        cut.Find("input[placeholder='Name, type, atmosphere...']").Input("Zephyr");

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".record-row");
            Assert.Single(rows);
            Assert.Contains("Zephyria", rows[0].TextContent);
            Assert.Contains("Showing 1 world", cut.Markup);
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear filters").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".record-row").Count);
            Assert.Contains("Showing 4 worlds", cut.Markup);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalWorlds()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var worlds = new List<World>();
        for (var index = 0; index < 121; index++)
        {
            worlds.Add(CreateWorld(200 + index, $"World {index:D3}", "Terran"));
        }

        ConfigureServices(CreateContext(additionalWorlds: worlds, includeMoonAndHabitat: false));
        var cut = Render<Worlds>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 120 worlds+", cut.Markup);
            Assert.DoesNotContain(cut.FindAll(".record-row").Select(row => row.TextContent), text => text.Contains("World 119", StringComparison.Ordinal));
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Load more").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 122 worlds", cut.Markup);
            Assert.Contains(cut.FindAll(".record-row").Select(row => row.TextContent), text => text.Contains("World 119", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void CreatesHabitatAndNavigatesToNewHabitatRecord()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var habitatService = new FakeSpaceHabitatService();
        ConfigureServices(CreateContext(includeMoonAndHabitat: false), habitatService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101");

        var cut = Render<Worlds>();
        cut.WaitForAssertion(() => Assert.Contains("Create habitat", cut.Markup));

        cut.Find("input[placeholder='Habitat name']").Change("New Ring");
        cut.Find(".habitat-create-form select").Change("2");
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Create habitat").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(101, habitatService.LastCreatedWorldId);
            Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&habitatId=9901", navigationManager.Uri, StringComparison.Ordinal);
        });
    }

    private void ConfigureServices(StarWinExplorerContext context, FakeSpaceHabitatService? habitatService = null)
    {
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());
        Services.AddSingleton<IStarWinSpaceHabitatService>(habitatService ?? new FakeSpaceHabitatService());
    }

    private static StarWinExplorerContext CreateContext(IEnumerable<World>? additionalWorlds = null, bool includeMoonAndHabitat = true)
    {
        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios",
            Coordinates = new Coordinates(10, 0, 0),
            MapCode = 4
        };

        system.AstralBodies.Add(new AstralBody
        {
            Id = 8001,
            Role = AstralBodyRole.Primary,
            Kind = AstralBodyKind.Star,
            Classification = "G2 V"
        });

        var primaryWorld = CreateWorld(101, "Eos", "Terran");
        primaryWorld.AtmosphereType = "Breathable";
        primaryWorld.AtmosphereComposition = "Nitrogen-Oxygen";
        primaryWorld.PrimaryAstralBodySequence = 0;
        primaryWorld.OrbitRadiusAu = 1.0;
        primaryWorld.Colony = new Colony
        {
            Id = 501,
            WorldId = 101,
            ColonyClass = "Capital",
            AllegianceId = 2,
            AllegianceName = "Orion Compact"
        };
        system.Worlds.Add(primaryWorld);

        if (includeMoonAndHabitat)
        {
            var moon = CreateWorld(102, "Selene", "Ice");
            moon.Kind = WorldKind.Moon;
            moon.ParentWorldId = 101;
            moon.OrbitRadiusKm = 380_000;
            moon.PrimaryAstralBodySequence = 0;
            system.Worlds.Add(moon);

            system.SpaceHabitats.Add(new SpaceHabitat
            {
                Id = 201,
                Name = "Arc Habitat",
                OrbitTargetKind = OrbitTargetKind.World,
                OrbitTargetId = 101,
                ControlledByEmpireId = 2,
                BuiltByEmpireId = 2,
                Population = 12_000_000
            });
        }

        if (additionalWorlds is not null)
        {
            foreach (var world in additionalWorlds)
            {
                system.Worlds.Add(world);
            }
        }

        var sector = new StarWinSector { Id = 7, Name = "Del Corra" };
        sector.Systems.Add(system);

        return new StarWinExplorerContext(
            [sector],
            sector,
            [],
            [new Empire { Id = 2, Name = "Orion Compact" }, new Empire { Id = 3, Name = "Zephyr League" }],
            []);
    }

    private static World CreateWorld(int id, string name, string worldType)
    {
        return new World
        {
            Id = id,
            Name = name,
            Kind = WorldKind.Planet,
            StarSystemId = 11,
            WorldType = worldType,
            AtmosphereType = "Thin",
            AtmosphereComposition = "Nitrogen",
            WaterType = "Liquid",
            DiameterKm = 12000,
            AtmosphericPressure = 1.0f,
            AverageTemperatureCelsius = 18,
            GravityEarthG = 0.98,
            MassEarthMasses = 1.02,
            EscapeVelocityKmPerSecond = 11.2,
            Hydrography = new Hydrography { IcePercent = 4, CloudPercent = 52 }
        };
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

    private sealed class FakeSpaceHabitatService : IStarWinSpaceHabitatService
    {
        public int LastCreatedWorldId { get; private set; }

        public Task<SpaceHabitat> CreateOrbitingAstralBodyAsync(int starSystemId, int astralBodySequence, int empireId, string? name, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SpaceHabitat> CreateOrbitingWorldAsync(int worldId, int empireId, string? name, CancellationToken cancellationToken = default)
        {
            LastCreatedWorldId = worldId;
            return Task.FromResult(new SpaceHabitat
            {
                Id = 9900 + (worldId - 100),
                Name = name ?? "New Habitat",
                OrbitTargetKind = OrbitTargetKind.World,
                OrbitTargetId = worldId,
                ControlledByEmpireId = empireId
            });
        }
    }
}
