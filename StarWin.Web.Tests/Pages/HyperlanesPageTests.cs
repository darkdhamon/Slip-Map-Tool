using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Layout;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class HyperlanesPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedHyperlaneInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var explorerContextService = new FakeExplorerContextService(CreateContext());
        ConfigureServices(explorerContextService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/hyperlanes?sectorId=7&systemId=11&hyperlaneId=2");

        var cut = Render<Hyperlanes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Saved Hyperlanes", cut.Markup);
            Assert.Contains("Helios", cut.Markup);
            Assert.Contains("Zephyria", cut.Markup);
            Assert.Contains("TL8 Advanced Hyperlane - 2.5 pc - 4 Months, 28 Days", cut.Markup);
            Assert.Contains("Advanced Hyperlane", cut.Markup);
            Assert.Contains("Orion Compact + Zephyr League", cut.Markup);
        });
    }

    [Fact]
    public void ShowsNewDraftUntilSavedHyperlaneIsExplicitlyChosen()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var explorerContextService = new FakeExplorerContextService(CreateContext());
        ConfigureServices(explorerContextService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/hyperlanes?sectorId=7&systemId=11");

        var cut = Render<Hyperlanes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Create saved hyperlane", cut.Markup);
            Assert.Empty(cut.FindAll(".hyperlane-record.active"));
            Assert.DoesNotContain("Delete hyperlane", cut.Markup);
        });

        cut.FindAll(".hyperlane-record")
            .Single(button => button.TextContent.Contains("Advanced Hyperlane", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Edit saved hyperlane", cut.Markup);
            Assert.Single(cut.FindAll(".hyperlane-record.active"));
            Assert.Contains("Delete hyperlane", cut.Markup);
            Assert.EndsWith("/sector-explorer/hyperlanes?sectorId=7&systemId=11&hyperlaneId=2", navigationManager.Uri, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalHyperlanes()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var explorerContextService = new FakeExplorerContextService(CreateContext(routeCount: 121));
        ConfigureServices(explorerContextService);

        var cut = Render<Hyperlanes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 120 saved hyperlanes+", cut.Markup);
            Assert.Equal(120, cut.FindAll(".hyperlane-record").Count);
        });

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Load more").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 121 saved hyperlanes", cut.Markup);
            Assert.Equal(121, cut.FindAll(".hyperlane-record").Count);
        });
    }

    [Fact]
    public void SaveHyperlaneCreatesManualRouteAndRefreshesSelection()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var routeService = new FakeSectorRouteService();
        var context = CreateContext();
        var explorerContextService = new FakeExplorerContextService(context);
        ConfigureServices(explorerContextService, routeService);

        var cut = Render<Hyperlanes>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "New draft").Click();
        cut.Find("input[maxlength='80']").Change("Manual Lane");
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Save hyperlane").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(routeService.SaveCalled);
            Assert.Equal("Manual Lane", routeService.LastSaveRequest?.TierName);
            Assert.Contains("Created saved hyperlane.", cut.Markup);
            Assert.Contains("Manual Lane", cut.Markup);
            Assert.EndsWith("/sector-explorer/hyperlanes?sectorId=7&systemId=11&hyperlaneId=3", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void DeleteHyperlaneRemovesSelectedRoute()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var routeService = new FakeSectorRouteService();
        var context = CreateContext();
        var explorerContextService = new FakeExplorerContextService(context);
        ConfigureServices(explorerContextService, routeService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/hyperlanes?sectorId=7&systemId=11&hyperlaneId=2");

        var cut = Render<Hyperlanes>();
        cut.WaitForAssertion(() => Assert.Contains("Advanced Hyperlane", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Delete hyperlane").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, routeService.DeletedRouteId);
            Assert.Contains("Deleted saved hyperlane.", cut.Markup);
            Assert.DoesNotContain(cut.FindAll(".hyperlane-record").Select(button => button.TextContent), text => text.Contains("Advanced Hyperlane", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void OpensSectorConfigurationFromHyperlanesPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var explorerContextService = new FakeExplorerContextService(CreateContext());
        ConfigureServices(explorerContextService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/hyperlanes?sectorId=7&systemId=11");

        var cut = Render<Hyperlanes>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Open sector configuration").Click();

        Assert.EndsWith("/sector-explorer/configuration?sectorId=7&systemId=11", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowsGenerationGateWhenSectorHasNoSavedHyperlanes()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var explorerContextService = new FakeExplorerContextService(CreateContext(routeCount: 0));
        ConfigureServices(explorerContextService);

        var cut = Render<Hyperlanes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This page unlocks after you generate the saved hyperlane cache for this sector.", cut.Markup);
            Assert.Contains("Generate hyperlanes", cut.Markup);
            Assert.Contains("Open sector configuration", cut.Markup);
            Assert.Contains("Basic Hyperlane up to 1 pc", cut.Markup);
            Assert.DoesNotContain("Add hyperlane", cut.Markup);
            Assert.DoesNotContain("Create saved hyperlane", cut.Markup);
            Assert.Empty(cut.FindAll(".hyperlane-record"));
        });
    }

    [Fact]
    public void GenerateHyperlanesUnlocksPageWhenNoSavedHyperlanesExist()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var routeService = new FakeSectorRouteService();
        var context = CreateContext(routeCount: 0);
        var explorerContextService = new FakeExplorerContextService(context);
        ConfigureServices(explorerContextService, routeService);

        var cut = Render<Hyperlanes>();
        cut.WaitForAssertion(() => Assert.Contains("Generate hyperlanes", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Generate hyperlanes").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(routeService.SaveCurrentRoutesCalled);
            Assert.Contains("Showing 1 saved hyperlane", cut.Markup);
            Assert.Contains("Create saved hyperlane", cut.Markup);
            Assert.Contains("Saved 1 hyperlane segment for Del Corra.", cut.Markup);
        });
    }

    [Fact]
    public void NewDraftClearsExplicitHyperlaneSelectionFromRoute()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var explorerContextService = new FakeExplorerContextService(CreateContext());
        ConfigureServices(explorerContextService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/hyperlanes?sectorId=7&systemId=11&hyperlaneId=2");

        var cut = Render<Hyperlanes>();
        cut.WaitForAssertion(() => Assert.Contains("Edit saved hyperlane", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "New draft").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Create saved hyperlane", cut.Markup);
            Assert.Empty(cut.FindAll(".hyperlane-record.active"));
            Assert.DoesNotContain("Delete hyperlane", cut.Markup);
            Assert.EndsWith("/sector-explorer/hyperlanes?sectorId=7&systemId=11", navigationManager.Uri, StringComparison.Ordinal);
        });
    }

    private void ConfigureServices(FakeExplorerContextService explorerContextService, FakeSectorRouteService? routeService = null)
    {
        var activeRouteService = routeService ?? new FakeSectorRouteService();
        activeRouteService.Context = explorerContextService.Context;

        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinExplorerContextService>(explorerContextService);
        Services.AddSingleton<IStarWinExplorerQueryService>(new ContextBackedExplorerQueryService(explorerContextService.Context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinSectorRouteService>(activeRouteService);
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());
    }

    private static StarWinExplorerContext CreateContext(int routeCount = 2)
    {
        var systems = new List<StarSystem>();
        var systemCount = Math.Max(routeCount + 1, 2);
        for (var index = 0; index < systemCount; index++)
        {
            systems.Add(new StarSystem
            {
                Id = 11 + index,
                SectorId = 7,
                Name = index switch
                {
                    0 => "Helios",
                    1 => "Zephyria",
                    _ => $"System {11 + index}"
                },
                Coordinates = new Coordinates((short)(index * 10), 0, 0),
                AllegianceId = index % 2 == 0 ? (ushort)2 : (ushort)3
            });
        }

        var sector = new StarWinSector
        {
            Id = 7,
            Name = "Del Corra",
            Configuration = new StarWin.Domain.Model.Entity.StarMap.SectorConfiguration
            {
                SectorId = 7,
                OffLaneMaximumDistanceParsecs = 2.5m,
                Tl9AndBelowMaximumConnectionsPerSystem = 4,
                AdditionalCrossEmpireConnectionsPerSystem = 1,
                Tl6HyperlaneName = "Basic Hyperlane",
                Tl6MaximumDistanceParsecs = 1.0m,
                Tl7HyperlaneName = "Enhanced Hyperlane",
                Tl7MaximumDistanceParsecs = 1.2m,
                Tl8HyperlaneName = "Advanced Hyperlane",
                Tl8MaximumDistanceParsecs = 1.4m,
                Tl9HyperlaneName = "Prime Hyperlane",
                Tl9MaximumDistanceParsecs = 1.6m,
                Tl10HyperlaneName = "Ascendant Hyperlane",
                Tl10MaximumDistanceParsecs = -1m
            }
        };
        foreach (var system in systems)
        {
            sector.Systems.Add(system);
        }

        for (var index = 0; index < routeCount; index++)
        {
            sector.SavedRoutes.Add(new SectorSavedRoute
            {
                Id = index + 1,
                SourceSystemId = systems[index].Id,
                TargetSystemId = systems[index + 1].Id,
                DistanceParsecs = 1.5m + index,
                TravelTimeYears = 0.4m + (index * 0.01m),
                TechnologyLevel = index == 1 ? (byte)8 : (byte)7,
                TierName = index == 1 ? "Advanced Hyperlane" : "Enhanced Hyperlane",
                PrimaryOwnerEmpireId = 2,
                PrimaryOwnerEmpireName = "Orion Compact",
                SecondaryOwnerEmpireId = index == 1 ? 3 : null,
                SecondaryOwnerEmpireName = index == 1 ? "Zephyr League" : string.Empty,
                IsUserPersisted = index % 2 == 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(index)
            });
        }

        var empires = new List<Empire>
        {
            new Empire { Id = 2, Name = "Orion Compact" },
            new Empire { Id = 3, Name = "Zephyr League" }
        };

        return new StarWinExplorerContext([sector], sector, [], empires);
    }

    private sealed class FakeExplorerContextService(StarWinExplorerContext context) : IStarWinExplorerContextService
    {
        public StarWinExplorerContext Context { get; } = context;

        public Task<StarWinExplorerContext> LoadShellAsync(int? preferredSectorId = null, bool includeReferenceData = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Context);
        }
    }

    private sealed class FakeSearchService : IStarWinSearchService
    {
        public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30) => [];
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

    private sealed class FakeSectorRouteService : IStarWinSectorRouteService
    {
        public StarWinExplorerContext Context { get; set; } = StarWinExplorerContext.Empty;
        public bool SaveCalled { get; private set; }
        public bool SaveCurrentRoutesCalled { get; private set; }
        public SectorManualRouteSaveRequest? LastSaveRequest { get; private set; }
        public int DeletedRouteId { get; private set; }

        public Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(int sectorId, IProgress<SectorRouteSaveProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            SaveCurrentRoutesCalled = true;
            var sector = Context.Sectors.First(item => item.Id == sectorId);
            var replacedExistingRoutes = sector.SavedRoutes.Count > 0;

            progress?.Report(new SectorRouteSaveProgress("Loading sector", "Reading the active sector and travel configuration.", 10));
            progress?.Report(new SectorRouteSaveProgress("Generating routes", $"Scanning {sector.Systems.Count:N0} systems for TL6+ colony hyperlane endpoints.", 35, 0, sector.Systems.Count));

            if (sector.SavedRoutes.Count == 0 && sector.Systems.Count >= 2)
            {
                sector.SavedRoutes.Add(new SectorSavedRoute
                {
                    Id = 1,
                    SectorId = sectorId,
                    SourceSystemId = sector.Systems[0].Id,
                    TargetSystemId = sector.Systems[1].Id,
                    DistanceParsecs = 1.0m,
                    TravelTimeYears = 0.4m,
                    TechnologyLevel = 6,
                    TierName = sector.Configuration.Tl6HyperlaneName,
                    GeneratedAtUtc = DateTime.UtcNow
                });
            }

            progress?.Report(new SectorRouteSaveProgress("Writing database", "Saving the updated route cache to the database.", 90));
            var finalRoutes = sector.SavedRoutes
                .Select(route => new SectorHyperlaneRouteDefinition(
                    route.SourceSystemId,
                    route.TargetSystemId,
                    (double)route.DistanceParsecs,
                    (double)route.TravelTimeYears,
                    route.TechnologyLevel,
                    route.TierName,
                    route.PrimaryOwnerEmpireId,
                    route.PrimaryOwnerEmpireName,
                    route.SecondaryOwnerEmpireId,
                    route.SecondaryOwnerEmpireName))
                .ToList();
            var networkReport = SectorRoutePlanner.BuildHyperlaneNetworkReport(
                sector.Systems.Select(system => system.Id),
                finalRoutes);

            progress?.Report(new SectorRouteSaveProgress("Routes saved", $"Stored {sector.SavedRoutes.Count:N0} cached hyperlane segment{(sector.SavedRoutes.Count == 1 ? string.Empty : "s")} for this sector.", 100));
            return Task.FromResult(new SectorRouteSaveResult(
                sectorId,
                sector.SavedRoutes.Count,
                sector.SavedRoutes.Count,
                sector.SavedRoutes.Count(route => route.IsUserPersisted),
                replacedExistingRoutes,
                DateTime.UtcNow,
                networkReport));
        }

        public Task<SectorSavedRoute> SaveManualRouteAsync(SectorManualRouteSaveRequest request, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            LastSaveRequest = request;

            var sector = Context.Sectors.First(sector => sector.Id == request.SectorId);
            SectorSavedRoute route;
            if (request.RouteId is int routeId)
            {
                route = sector.SavedRoutes.First(item => item.Id == routeId);
            }
            else
            {
                route = new SectorSavedRoute
                {
                    Id = sector.SavedRoutes.Count == 0 ? 1 : sector.SavedRoutes.Max(item => item.Id) + 1
                };
                sector.SavedRoutes.Add(route);
            }

            route.SourceSystemId = request.SourceSystemId;
            route.TargetSystemId = request.TargetSystemId;
            route.DistanceParsecs = request.DistanceParsecs;
            route.TravelTimeYears = request.TravelTimeYears;
            route.TechnologyLevel = request.TechnologyLevel;
            route.TierName = request.TierName;
            route.PrimaryOwnerEmpireId = request.PrimaryOwnerEmpireId;
            route.PrimaryOwnerEmpireName = request.PrimaryOwnerEmpireName;
            route.SecondaryOwnerEmpireId = request.SecondaryOwnerEmpireId;
            route.SecondaryOwnerEmpireName = request.SecondaryOwnerEmpireName;
            route.IsUserPersisted = request.IsUserPersisted;
            route.GeneratedAtUtc = DateTime.UtcNow;

            return Task.FromResult(route);
        }

        public Task DeleteSavedRouteAsync(int sectorId, int routeId, CancellationToken cancellationToken = default)
        {
            DeletedRouteId = routeId;
            var sector = Context.Sectors.First(item => item.Id == sectorId);
            var route = sector.SavedRoutes.First(item => item.Id == routeId);
            sector.SavedRoutes.Remove(route);
            return Task.CompletedTask;
        }
    }
}
