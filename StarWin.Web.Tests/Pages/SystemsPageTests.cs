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

public sealed class SystemsPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedSystemInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/systems?sectorId=7&systemId=11");

        var cut = Render<Systems>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("System survey", cut.Markup);
            Assert.Contains("Helios", cut.Markup);
            Assert.Contains("World count", cut.Markup);
            Assert.Contains("Primary", cut.Markup);
        });
    }

    [Fact]
    public void ShowsSelectionPromptUntilSystemIsExplicitlyChosen()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/systems?sectorId=7");

        var cut = Render<Systems>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select a system to load its details.", cut.Markup);
            Assert.Empty(cut.FindAll(".record-row.active"));
            Assert.DoesNotContain("System survey", cut.Markup);
        });

        cut.FindAll(".record-row")
            .Single(button => button.TextContent.Contains("Helios", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("System survey", cut.Markup);
            Assert.Contains("Helios", cut.Markup);
            Assert.Single(cut.FindAll(".record-row.active"));
            Assert.EndsWith("/sector-explorer/systems?sectorId=7&systemId=11", navigationManager.Uri, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void LoadsRequestedSystemDetailOnlyOnceOnInitialRender()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var context = CreateContext();
        var queryService = new CountingSystemQueryService(context);
        ConfigureServices(context, queryService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/systems?sectorId=7&systemId=11");

        var cut = Render<Systems>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("System survey", cut.Markup);
            Assert.Equal(1, queryService.SystemDetailLoadCount);
        });
    }

    [Fact]
    public void FiltersSystemsBySearchQueryAndClearsFilters()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext(additionalSystems: [CreateSystem(12, "Zephyria", 3, worldName: "Zephyr")]));

        var cut = Render<Systems>();
        cut.Find("input[placeholder='Name, ID, coordinates...']").Input("Zephyr");

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".record-row");
            Assert.Single(rows);
            Assert.Contains("Zephyria", rows[0].TextContent);
            Assert.Contains("Showing 1 system", cut.Markup);
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Clear filters").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".record-row").Count);
            Assert.Contains("Showing 2 systems", cut.Markup);
        });
    }

    [Fact]
    public void FiltersSystemsByAllegianceWhenFilterIsApplied()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext(additionalSystems: [CreateSystem(12, "Kharon", 3)]));

        var cut = Render<Systems>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Show filters").Click();
        cut.Find("input[placeholder='All allegiances']").Input("3 - Zephyr League");

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll(".record-row");
            Assert.Single(rows);
            Assert.Contains("Kharon", rows[0].TextContent);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalSystems()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var systems = new List<StarSystem>();
        for (var index = 0; index < 121; index++)
        {
            systems.Add(CreateSystem(100 + index, $"System {index:D3}", 2));
        }

        ConfigureServices(CreateContext(additionalSystems: systems));
        var cut = Render<Systems>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 120 systems+", cut.Markup);
            Assert.DoesNotContain(cut.FindAll(".record-row").Select(row => row.TextContent), text => text.Contains("System 119", StringComparison.Ordinal));
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Load more").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 122 systems", cut.Markup);
            Assert.Contains(cut.FindAll(".record-row").Select(row => row.TextContent), text => text.Contains("System 119", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void NavigatesToWorldsPageFromSatelliteLinks()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/systems?sectorId=7&systemId=11");

        var cut = Render<Systems>();
        cut.WaitForAssertion(() => Assert.Contains("Eos", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Eos", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&worldId=111", navigationManager.Uri, StringComparison.Ordinal);
    }

    private void ConfigureServices(StarWinExplorerContext context, IStarWinExplorerQueryService? queryService = null)
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinExplorerQueryService>(queryService ?? new ContextBackedExplorerQueryService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());
        Services.AddSingleton<IStarWinSpaceHabitatService>(new FakeSpaceHabitatService());
    }

    private static StarWinExplorerContext CreateContext(IEnumerable<StarSystem>? additionalSystems = null)
    {
        var systems = new List<StarSystem>
        {
            CreateSystem(11, "Helios", 2, worldName: "Eos")
        };

        if (additionalSystems is not null)
        {
            systems.AddRange(additionalSystems);
        }

        var sector = new StarWinSector { Id = 7, Name = "Del Corra" };
        foreach (var system in systems)
        {
            sector.Systems.Add(system);
        }

        var empires = new List<Empire>
        {
            new Empire { Id = 2, Name = "Orion Compact" },
            new Empire { Id = 3, Name = "Zephyr League" }
        };

        return new StarWinExplorerContext([sector], sector, [], empires);
    }

    private static StarSystem CreateSystem(int systemId, string name, ushort allegianceId, string worldName = "Eos")
    {
        var system = new StarSystem
        {
            Id = systemId,
            SectorId = 7,
            Name = name,
            Coordinates = new Coordinates((short)(systemId * 10), 0, 0),
            AllegianceId = allegianceId,
            MapCode = 4
        };

        system.AstralBodies.Add(new AstralBody
        {
            Id = 8000 + systemId,
            Role = AstralBodyRole.Primary,
            Kind = AstralBodyKind.Star,
            Classification = "G2 V",
            Luminosity = 1.1,
            SolarMasses = 1.0
        });

        system.Worlds.Add(new World
        {
            Id = 100 + systemId,
            Name = worldName,
            Kind = WorldKind.Planet,
            StarSystemId = systemId,
            PrimaryAstralBodySequence = 0,
            WorldType = "Terran",
            AtmosphereType = "Breathable"
        });

        system.SpaceHabitats.Add(new SpaceHabitat
        {
            Id = 200 + systemId,
            Name = $"Habitat {name}",
            OrbitTargetKind = OrbitTargetKind.AstralBody,
            OrbitTargetId = 0,
            ControlledByEmpireId = allegianceId,
            Population = 12_000_000
        });

        return system;
    }

    private sealed class FakeExplorerContextService(StarWinExplorerContext context) : IStarWinExplorerContextService
    {
        public Task<StarWinExplorerContext> LoadShellAsync(int? preferredSectorId = null, bool includeReferenceData = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context);
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
        public Task<SpaceHabitat> CreateOrbitingAstralBodyAsync(int starSystemId, int astralBodySequence, int empireId, string? name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SpaceHabitat
            {
                Id = 9000 + starSystemId,
                Name = name ?? "New Habitat",
                OrbitTargetKind = OrbitTargetKind.AstralBody,
                OrbitTargetId = astralBodySequence,
                ControlledByEmpireId = empireId
            });
        }

        public Task<SpaceHabitat> CreateOrbitingWorldAsync(int worldId, int empireId, string? name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SpaceHabitat
            {
                Id = 9500 + worldId,
                Name = name ?? "New Habitat",
                OrbitTargetKind = OrbitTargetKind.World,
                OrbitTargetId = worldId,
                ControlledByEmpireId = empireId
            });
        }
    }

    private sealed class CountingSystemQueryService(StarWinExplorerContext context) : IStarWinExplorerQueryService
    {
        private readonly ContextBackedExplorerQueryService inner = new(context);

        public int SystemDetailLoadCount { get; private set; }

        public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadSectorOverviewAsync(sectorId, cancellationToken);
        public Task<ExplorerSectorEntityUsage> LoadSectorEntityUsageAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadSectorEntityUsageAsync(sectorId, cancellationToken);
        public Task<IReadOnlyList<StarWinSearchResult>> SearchSectorAsync(int sectorId, string query, int maxResults = 30, CancellationToken cancellationToken = default) => inner.SearchSectorAsync(sectorId, query, maxResults, cancellationToken);
        public Task<int?> ResolveSystemIdAsync(int sectorId, int? worldId = null, int? colonyId = null, int? habitatId = null, CancellationToken cancellationToken = default) => inner.ResolveSystemIdAsync(sectorId, worldId, colonyId, habitatId, cancellationToken);
        public Task<IReadOnlyList<ExplorerLookupOption>> LoadSectorEmpireOptionsAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadSectorEmpireOptionsAsync(sectorId, cancellationToken);
        public Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadAlienRaceFilterOptionsAsync(sectorId, cancellationToken);
        public Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default) => inner.LoadAlienRaceListPageAsync(request, cancellationToken);
        public Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default) => inner.LoadAlienRaceListItemAsync(sectorId, raceId, cancellationToken);
        public Task<ExplorerAlienRaceDetail?> LoadAlienRaceDetailAsync(int sectorId, int raceId, CancellationToken cancellationToken = default) => inner.LoadAlienRaceDetailAsync(sectorId, raceId, cancellationToken);
        public Task<ExplorerSystemFilterOptions> LoadSystemFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadSystemFilterOptionsAsync(sectorId, cancellationToken);
        public Task<ExplorerSystemListPage> LoadSystemListPageAsync(ExplorerSystemListPageRequest request, CancellationToken cancellationToken = default) => inner.LoadSystemListPageAsync(request, cancellationToken);
        public Task<ExplorerSystemListItem?> LoadSystemListItemAsync(int sectorId, int systemId, CancellationToken cancellationToken = default) => inner.LoadSystemListItemAsync(sectorId, systemId, cancellationToken);

        public Task<ExplorerSystemDetail?> LoadSystemDetailAsync(int sectorId, int systemId, CancellationToken cancellationToken = default)
        {
            SystemDetailLoadCount++;
            return inner.LoadSystemDetailAsync(sectorId, systemId, cancellationToken);
        }

        public Task<ExplorerWorldsWorkspace?> LoadWorldsWorkspaceAsync(int sectorId, int systemId, CancellationToken cancellationToken = default) => inner.LoadWorldsWorkspaceAsync(sectorId, systemId, cancellationToken);
        public Task<ExplorerColonyFilterOptions> LoadColonyFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadColonyFilterOptionsAsync(sectorId, cancellationToken);
        public Task<ExplorerColonyListPage> LoadColonyListPageAsync(ExplorerColonyListPageRequest request, CancellationToken cancellationToken = default) => inner.LoadColonyListPageAsync(request, cancellationToken);
        public Task<ExplorerColonyListItem?> LoadColonyListItemAsync(int sectorId, int colonyId, CancellationToken cancellationToken = default) => inner.LoadColonyListItemAsync(sectorId, colonyId, cancellationToken);
        public Task<ExplorerColonyDetail?> LoadColonyDetailAsync(int sectorId, int colonyId, CancellationToken cancellationToken = default) => inner.LoadColonyDetailAsync(sectorId, colonyId, cancellationToken);
        public Task<ExplorerReligionFilterOptions> LoadReligionFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadReligionFilterOptionsAsync(sectorId, cancellationToken);
        public Task<ExplorerReligionListPage> LoadReligionListPageAsync(ExplorerReligionListPageRequest request, CancellationToken cancellationToken = default) => inner.LoadReligionListPageAsync(request, cancellationToken);
        public Task<ExplorerReligionListItem?> LoadReligionListItemAsync(int sectorId, int religionId, CancellationToken cancellationToken = default) => inner.LoadReligionListItemAsync(sectorId, religionId, cancellationToken);
        public Task<ExplorerReligionDetail?> LoadReligionDetailAsync(int sectorId, int religionId, CancellationToken cancellationToken = default) => inner.LoadReligionDetailAsync(sectorId, religionId, cancellationToken);
        public Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadEmpireFilterOptionsAsync(sectorId, cancellationToken);
        public Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default) => inner.LoadEmpireListPageAsync(request, cancellationToken);
        public Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default) => inner.LoadEmpireListItemAsync(sectorId, empireId, cancellationToken);
        public Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default) => inner.LoadEmpireDetailAsync(sectorId, empireId, cancellationToken);
        public Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadTimelineEventTypesAsync(sectorId, cancellationToken);
        public Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default) => inner.LoadTimelinePageAsync(request, cancellationToken);
        public Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default) => inner.LoadTimelineEventDetailAsync(eventId, cancellationToken);
        public Task<ExplorerHyperlanePageState?> LoadHyperlanePageStateAsync(int sectorId, CancellationToken cancellationToken = default) => inner.LoadHyperlanePageStateAsync(sectorId, cancellationToken);
    }
}
