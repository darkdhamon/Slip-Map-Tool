using System.Reflection;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
using StarWin.Web.Components.Explorer;
using StarWin.Web.Components.Layout;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class SectorExplorerPageTests : BunitContext
{
    [Fact]
    public void OverviewDefersMapWorkspaceUntilAfterInitialRender()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector)
        {
            DelayReload = true
        };

        ConfigureServices(sector, workspace);

        var cut = Render<SectorExplorer>();

        Assert.DoesNotContain("Plot hyperlane route", cut.Markup);

        cut.WaitForAssertion(() => Assert.Contains("Loading route-planning workspace", cut.Markup));

        workspace.ReleaseReload();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Load 3D map", cut.Markup);
            Assert.DoesNotContain("Loading route-planning workspace", cut.Markup);
        });

    }

    [Fact]
    public void OverviewSelectsFirstSectorAndLoadsMetricsWhenNoQueryOrStoredSessionExists()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector)
        {
            DelayReload = true
        };

        ConfigureServices(sector, workspace);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer");

        var cut = Render<SectorExplorer>();

        workspace.ReleaseReload();

        cut.WaitForAssertion(() =>
        {
            Assert.EndsWith("/sector-explorer?sectorId=7&systemId=11", navigationManager.Uri, StringComparison.Ordinal);
            Assert.Contains("<strong>2</strong>", cut.Markup);
            Assert.Contains("<strong>1</strong>", cut.Markup);
            Assert.DoesNotContain("Sector 0 is not available in the workspace.", cut.Markup);
        });
    }

    [Fact]
    public async Task OverviewMetricsLoadOncePerSectorWhileSameSectorRequestsAreInFlight()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector);
        var queryService = new FakeExplorerQueryService(sector);
        ConfigureServices(sector, workspace, queryService);

        var cut = Render<SectorExplorer>();
        cut.WaitForAssertion(() => Assert.Contains("<strong>2</strong>", cut.Markup));

        var baselineCallCount = queryService.LoadSectorOverviewCallCount;
        queryService.DelayOverviewLoads = true;

        var loadMethod = typeof(SectorExplorer).GetMethod("LoadSectorOverviewDataAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(loadMethod);

        var loadOne = cut.InvokeAsync(() => (Task)loadMethod!.Invoke(cut.Instance, [99, CancellationToken.None])!);
        var loadTwo = cut.InvokeAsync(() => (Task)loadMethod!.Invoke(cut.Instance, [99, CancellationToken.None])!);

        cut.WaitForAssertion(() => Assert.Equal(baselineCallCount + 1, queryService.LoadSectorOverviewCallCount));

        queryService.ReleaseOverviewLoad();
        await Task.WhenAll(loadOne, loadTwo);

        Assert.Equal(baselineCallCount + 1, queryService.LoadSectorOverviewCallCount);
    }

    [Fact]
    public void MapWorkspaceShowsSingleSharedLoadingModalWhileDeferredWorkspaceLoads()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector)
        {
            DelayReload = true
        };

        ConfigureServices(sector, workspace);

        var cut = Render<SectorExplorerMapWorkspace>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SystemId, 11));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".loading-modal"));
            Assert.Contains("Preparing map workspace", cut.Markup);
            Assert.Contains("Preparing route-planning workspace", cut.Markup);
        });

        workspace.ReleaseReload();

        cut.WaitForAssertion(() => Assert.Contains("Load 3D map", cut.Markup));
    }

    [Fact]
    public async Task MapWorkspaceUpdatesOverviewQueryWithoutParentCallback()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer?sectorId=7&systemId=11");

        var cut = Render<SectorExplorerMapWorkspace>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SystemId, 11));

        cut.WaitForAssertion(() => Assert.Contains("Load 3D map", cut.Markup));
        await cut.InvokeAsync(() => cut.Instance.SelectSystemFromMap(12));

        Assert.EndsWith("/sector-explorer?sectorId=7&systemId=12", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OverviewSystemSelectorRetargetsMapAndPreservesSystemIdInUrl()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer?sectorId=7&systemId=11");

        var cut = Render<SectorExplorer>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<SectorExplorerMapWorkspace>()));

        var handler = typeof(SectorExplorer).GetMethod("HandleSelectedSystemTextChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(handler);

        await cut.InvokeAsync(() => (Task)handler!.Invoke(cut.Instance, ["12 - Selene"])!);

        cut.WaitForAssertion(() =>
        {
            Assert.EndsWith("/sector-explorer?sectorId=7&systemId=12", navigationManager.Uri, StringComparison.Ordinal);
            Assert.Equal(12, cut.FindComponent<SectorExplorerMapWorkspace>().Instance.SystemId);
        });
    }

    [Fact]
    public async Task MapSelectionUpdatesOverviewSystemSelector()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer?sectorId=7&systemId=11");

        var cut = Render<SectorExplorer>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<SectorExplorerMapWorkspace>()));

        var mapWorkspace = cut.FindComponent<SectorExplorerMapWorkspace>();
        await cut.InvokeAsync(() => mapWorkspace.Instance.SelectSystemFromMap(12));

        var selectedSystemTextField = typeof(SectorExplorer).GetField("selectedSystemText", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(selectedSystemTextField);

        cut.WaitForAssertion(() =>
        {
            Assert.EndsWith("/sector-explorer?sectorId=7&systemId=12", navigationManager.Uri, StringComparison.Ordinal);
            Assert.Equal("12 - Selene", selectedSystemTextField!.GetValue(cut.Instance));
            Assert.Equal(12, cut.FindComponent<SectorExplorerMapWorkspace>().Instance.SystemId);
        });
    }

    [Fact]
    public async Task MapWorkspaceNavigatesDirectlyToSystemsPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer?sectorId=7&systemId=12");

        var cut = Render<SectorExplorerMapWorkspace>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SystemId, 12));

        cut.WaitForAssertion(() => Assert.Contains("Load 3D map", cut.Markup));
        var openRecordMethod = typeof(SectorExplorerMapWorkspace).GetMethod("OpenSelectedSystemRecordAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(openRecordMethod);

        await cut.InvokeAsync(() => (Task)openRecordMethod!.Invoke(cut.Instance, [])!);

        Assert.EndsWith("/sector-explorer/systems?sectorId=7&systemId=12", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void MapWorkspaceSkipsPersistentDynamicRouteFallbackPromptWhenSavedRoutesAreMissing()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector(includeSavedRoutes: false);
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var cut = Render<SectorExplorerMapWorkspace>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SystemId, 11));

        cut.WaitForAssertion(() => Assert.Contains("Load 3D map", cut.Markup));
        Assert.DoesNotContain("Open sector configuration", cut.Markup);
        Assert.DoesNotContain("No saved routes yet. The map is generating them dynamically.", cut.Markup);
    }

    [Fact]
    public void MapWorkspaceUsesShortDynamicRouteFallbackOverlayCopy()
    {
        var noteMethod = typeof(SectorExplorerMapWorkspace).GetMethod("GetSavedRouteGenerationLoadingNote", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(noteMethod);

        var note = noteMethod!.Invoke(null, []) as string;

        Assert.Equal("No saved routes yet. The map is generating them dynamically. Use Save Current Routes in Sector Configuration to speed up future loads.", note);
    }

    [Fact]
    public async Task MapWorkspaceHidesDynamicRouteFallbackHintAfterMapRenderCompletes()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector(includeSavedRoutes: false);
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var cut = Render<SectorExplorerMapWorkspace>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SystemId, 11));

        cut.WaitForAssertion(() => Assert.Contains("Load 3D map", cut.Markup));

        var sectorMapRequestedField = typeof(SectorExplorerMapWorkspace).GetField("sectorMapRequested", BindingFlags.Instance | BindingFlags.NonPublic);
        var renderedOverviewField = typeof(SectorExplorerMapWorkspace).GetField("renderedOverview", BindingFlags.Instance | BindingFlags.NonPublic);
        var stateHasChangedMethod = typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(sectorMapRequestedField);
        Assert.NotNull(renderedOverviewField);
        Assert.NotNull(stateHasChangedMethod);

        await cut.InvokeAsync(() =>
        {
            sectorMapRequestedField!.SetValue(cut.Instance, true);
            renderedOverviewField!.SetValue(cut.Instance, false);
            stateHasChangedMethod!.Invoke(cut.Instance, []);
        });

        cut.WaitForAssertion(() => Assert.Contains("No saved routes yet. The map is generating them dynamically. Use Save Current Routes in Sector Configuration to speed up future loads.", cut.Markup));

        await cut.InvokeAsync(() =>
        {
            sectorMapRequestedField!.SetValue(cut.Instance, true);
            renderedOverviewField!.SetValue(cut.Instance, true);
            stateHasChangedMethod!.Invoke(cut.Instance, []);
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Open system record", cut.Markup);
            Assert.DoesNotContain("No saved routes yet. The map is generating them dynamically. Use Save Current Routes in Sector Configuration to speed up future loads.", cut.Markup);
            Assert.DoesNotContain("Open sector configuration", cut.Markup);
        });
    }

    [Fact]
    public void MapWorkspaceBuildsPayloadWithExpectedRendererFields()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var sector = CreateSector();
        var workspace = new FakeWorkspace(sector);
        ConfigureServices(sector, workspace);

        var cut = Render<SectorExplorerMapWorkspace>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SystemId, 11));

        cut.WaitForAssertion(() => Assert.Contains("Load 3D map", cut.Markup));

        var buildSectorMap = typeof(SectorExplorerMapWorkspace).GetMethod("BuildSectorMap", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(buildSectorMap);

        var payload = buildSectorMap!.Invoke(cut.Instance, [sector, 11]);
        var json = JsonSerializer.Serialize(payload);

        Assert.Contains("\"viewDistanceParsecs\":5", json);
        Assert.Contains("\"fadeStartParsecs\":4", json);
        Assert.Contains("\"routeActive\":false", json);
        Assert.Contains("\"routeSystemIds\":[]", json);
        Assert.Contains("\"routePath\":[]", json);
        Assert.Contains("\"distanceFromFocus\":0", json);
        Assert.Contains("\"showLabel\":true", json);
        Assert.Contains("\"isRouteSystem\":false", json);
        Assert.Contains("\"bodies\":[{", json);
        Assert.Contains("\"kind\":\"Star\"", json);
        Assert.Contains("\"classification\":\"G2 V\"", json);
        Assert.Contains("\"luminosity\":1.08", json);
        Assert.Contains("\"solarMasses\":1.01", json);
    }

    private void ConfigureServices(StarWinSector sector, FakeWorkspace workspace, FakeExplorerQueryService? queryService = null)
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinWorkspace>(workspace);
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(sector));
        Services.AddSingleton<IStarWinExplorerQueryService>(queryService ?? new FakeExplorerQueryService(sector));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
    }

    private static StarWinSector CreateSector(bool includeSavedRoutes = true)
    {
        var sector = new StarWinSector
        {
            Id = 7,
            Name = "Del Corra",
            Configuration = new StarWin.Domain.Model.Entity.StarMap.SectorConfiguration
            {
                SectorId = 7,
                OffLaneMaximumDistanceParsecs = 2m
            }
        };

        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios",
            Coordinates = new Coordinates(0, 0, 0),
            AllegianceId = 8
        };
        system.AstralBodies.Add(new AstralBody
        {
            Id = 901,
            Role = AstralBodyRole.Primary,
            Kind = AstralBodyKind.Star,
            Classification = "G2 V",
            Luminosity = 1.08,
            SolarMasses = 1.01
        });

        var world = new World
        {
            Id = 101,
            Name = "Eos",
            StarSystemId = 11
        };
        world.Colony = new Colony
        {
            Id = 501,
            Name = "Helios Prime",
            WorldId = 101,
            RaceId = 3,
            AllegianceId = 8
        };

        system.Worlds.Add(world);
        sector.Systems.Add(system);
        sector.Systems.Add(new StarSystem
        {
            Id = 12,
            SectorId = 7,
            Name = "Selene",
            Coordinates = new Coordinates(1, 0, 0),
            AllegianceId = 8
        });
        if (includeSavedRoutes)
        {
            sector.SavedRoutes.Add(new SectorSavedRoute
            {
                Id = 1,
                SectorId = 7,
                SourceSystemId = 11,
                TargetSystemId = 12,
                DistanceParsecs = 1m,
                TravelTimeYears = 0.01m,
                TechnologyLevel = 6,
                TierName = "Basic Hyperlane"
            });
        }

        return sector;
    }

    private sealed class FakeWorkspace : IStarWinWorkspace
    {
        private readonly StarWinSector sector;
        private readonly TaskCompletionSource reloadTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeWorkspace(StarWinSector sector)
        {
            this.sector = sector;
            CurrentSector = sector;
        }

        public bool DelayReload { get; set; }

        public bool IsLoaded { get; private set; }

        public IReadOnlyList<StarWinSector> Sectors => IsLoaded ? [sector] : [];

        public StarWinSector CurrentSector { get; private set; }

        public IReadOnlyList<AlienRace> AlienRaces => [new AlienRace { Id = 3, Name = "Krell" }];

        public IReadOnlyList<Empire> Empires => [new Empire { Id = 8, Name = "Orion Compact" }];

        public CivilizationGeneratorSettings CivilizationSettings { get; } = new();

        public ArmyGeneratorSettings ArmySettings { get; } = new();

        public GurpsTemplate PreviewGurpsTemplate { get; } = new();

        public async Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            if (DelayReload)
            {
                using var registration = cancellationToken.Register(() => reloadTcs.TrySetCanceled(cancellationToken));
                await reloadTcs.Task;
            }

            IsLoaded = true;
            CurrentSector = sector;
        }

        public void ReleaseReload()
        {
            reloadTcs.TrySetResult();
        }
    }

    private sealed class FakeExplorerContextService : IStarWinExplorerContextService
    {
        private readonly StarWinExplorerContext context;

        public FakeExplorerContextService(StarWinSector sector)
        {
            context = new StarWinExplorerContext(
                [sector],
                sector,
                [new AlienRace { Id = 3, Name = "Krell" }],
                [new Empire { Id = 8, Name = "Orion Compact" }]);
        }

        public Task<StarWinExplorerContext> LoadShellAsync(int? preferredSectorId = null, bool includeReferenceData = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context);
        }
    }

    private sealed class FakeExplorerQueryService : IStarWinExplorerQueryService
    {
        private readonly ExplorerSectorOverviewData overviewData;
        private readonly TaskCompletionSource overviewLoadTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeExplorerQueryService(StarWinSector sector)
        {
            overviewData = new ExplorerSectorOverviewData(
                sector.Id,
                sector.Systems.Count,
                sector.Systems.SelectMany(system => system.Worlds).Count(),
                sector.Systems.SelectMany(system => system.Worlds).Count(world => world.Colony is not null),
                1,
                1);
        }

        public bool DelayOverviewLoads { get; set; }

        public int LoadSectorOverviewCallCount { get; private set; }

        public async Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            LoadSectorOverviewCallCount++;

            if (DelayOverviewLoads)
            {
                using var registration = cancellationToken.Register(() => overviewLoadTcs.TrySetCanceled(cancellationToken));
                await overviewLoadTcs.Task;
            }

            return overviewData with { SectorId = sectorId };
        }

        public void ReleaseOverviewLoad()
        {
            overviewLoadTcs.TrySetResult();
        }

        public Task<ExplorerSectorEntityUsage> LoadSectorEntityUsageAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerSectorEntityUsage(sectorId, [], []));
        }

        public Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerAlienRaceDetail?> LoadAlienRaceDetailAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerEmpireFilterOptions([]));
        }

        public Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerEmpireListPage([], false));
        }

        public Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExplorerEmpireListItem?>(null);
        }

        public Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExplorerEmpireDetail?>(null);
        }

        public Task<ExplorerReligionFilterOptions> LoadReligionFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerReligionFilterOptions([]));
        }

        public Task<ExplorerReligionListPage> LoadReligionListPageAsync(ExplorerReligionListPageRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerReligionListPage([], false));
        }

        public Task<ExplorerReligionListItem?> LoadReligionListItemAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExplorerReligionListItem?>(null);
        }

        public Task<ExplorerReligionDetail?> LoadReligionDetailAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExplorerReligionDetail?>(null);
        }

        public Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerTimelinePage([], false));
        }

        public Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExplorerTimelineEventDetail?>(null);
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
            throw new NotSupportedException();
        }
    }

    private sealed class FakeEntityNameService : IStarWinEntityNameService
    {
        public Task<string> SaveNameAsync(EntityNoteTargetKind targetKind, int targetId, string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(name);
        }
    }
}
