using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Pages;
using SectorConfigModel = StarWin.Domain.Model.Entity.StarMap.SectorConfiguration;
using SectorConfigurationPage = StarWin.Web.Components.Pages.SectorConfiguration;

namespace StarWin.Web.Tests.Pages;

public sealed class SectorConfigurationPageTests : BunitContext
{
    [Fact]
    public void RendersDedicatedConfigurationPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/configuration?sectorId=7&systemId=11");

        var cut = Render<SectorConfigurationPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sector Configuration", cut.Markup);
            Assert.Contains("Del Corra", cut.Markup);
            Assert.Contains("Save configuration", cut.Markup);
            Assert.Contains("Saved route report", cut.Markup);
        });
    }

    [Fact]
    public void SaveConfigurationUsesConfigurationServices()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var configService = new FakeSectorConfigurationService();
        ConfigureServices(CreateContext(), configService: configService);

        var cut = Render<SectorConfigurationPage>();
        cut.Find("input[maxlength='160']").Change("Del Corra Prime");
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Save configuration").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Del Corra Prime", configService.SavedSectorName);
            Assert.NotNull(configService.SavedConfiguration);
            Assert.Contains("Del Corra Prime saved.", cut.Markup);
        });
    }

    [Fact]
    public void SaveCurrentRoutesDisplaysSuccessStatus()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var routeService = new FakeSectorRouteService();
        ConfigureServices(CreateContext(), routeService: routeService);

        var cut = Render<SectorConfigurationPage>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Update Current Routes").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(routeService.SaveCalled);
            Assert.Contains("Updated 3 saved hyperlane segments", cut.Markup);
        });
    }

    [Fact]
    public void ConvertIndependentColoniesDisplaysConversionSummary()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var colonyService = new FakeIndependentColonyService();
        ConfigureServices(CreateContext(), colonyService: colonyService);

        var cut = Render<SectorConfigurationPage>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Convert independent colonies").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(colonyService.ConvertCalled);
            Assert.Contains("Created 1 empire and assigned 2 colonies.", cut.Markup);
        });
    }

    private void ConfigureServices(
        StarWinExplorerContext context,
        FakeSectorConfigurationService? configService = null,
        FakeSectorRouteService? routeService = null,
        FakeIndependentColonyService? colonyService = null)
    {
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinSectorConfigurationService>(configService ?? new FakeSectorConfigurationService());
        Services.AddSingleton<IStarWinSectorRouteService>(routeService ?? new FakeSectorRouteService());
        Services.AddSingleton<IStarWinIndependentColonyService>(colonyService ?? new FakeIndependentColonyService());
    }

    private static StarWinExplorerContext CreateContext()
    {
        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios"
        };

        var sector = new StarWinSector
        {
            Id = 7,
            Name = "Del Corra",
            Configuration = new SectorConfigModel
            {
                SectorId = 7,
                OffLaneMaximumDistanceParsecs = 2.5m,
                UpdatedAtUtc = new DateTime(2026, 4, 25, 12, 0, 0, DateTimeKind.Utc)
            }
        };
        sector.Systems.Add(system);
        sector.SavedRoutes.Add(new SectorSavedRoute
        {
            Id = 1,
            SourceSystemId = 11,
            TargetSystemId = 12,
            DistanceParsecs = 1.2m,
            TravelTimeYears = 0.4m,
            TechnologyLevel = 7,
            TierName = "Enhanced Hyperlane",
            GeneratedAtUtc = DateTime.UtcNow
        });

        var empire = new Empire { Id = 2, Name = "Orion Compact" };
        return new StarWinExplorerContext([sector], sector, [], [empire], []);
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

    private sealed class FakeSectorConfigurationService : IStarWinSectorConfigurationService
    {
        public string? SavedSectorName { get; private set; }
        public SectorConfigModel? SavedConfiguration { get; private set; }

        public Task<string> SaveSectorNameAsync(int sectorId, string name, CancellationToken cancellationToken = default)
        {
            SavedSectorName = name;
            return Task.FromResult(name);
        }

        public Task<SectorConfigModel> SaveHyperlaneSettingsAsync(int sectorId, SectorConfigModel configuration, CancellationToken cancellationToken = default)
        {
            SavedConfiguration = configuration;
            return Task.FromResult(configuration);
        }
    }

    private sealed class FakeSectorRouteService : IStarWinSectorRouteService
    {
        public bool SaveCalled { get; private set; }

        public async Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(int sectorId, IProgress<SectorRouteSaveProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            progress?.Report(new SectorRouteSaveProgress("Generating routes", "Calculating route graph.", 65, 8, 12));
            await Task.Yield();
            return new SectorRouteSaveResult(
                sectorId,
                3,
                2,
                1,
                true,
                DateTime.UtcNow,
                new SectorHyperlaneNetworkReport(3, [4, 2, 1], 1, 4, 3));
        }

        public Task<SectorSavedRoute> SaveManualRouteAsync(SectorManualRouteSaveRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteSavedRouteAsync(int sectorId, int routeId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeIndependentColonyService : IStarWinIndependentColonyService
    {
        public bool ConvertCalled { get; private set; }

        public Task<IndependentColonyConversionResult> ConvertIndependentColoniesAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            ConvertCalled = true;
            return Task.FromResult(new IndependentColonyConversionResult(
                [new Empire { Id = 10, Name = "New Independent Empire" }],
                [
                    new IndependentColonyAssignment(1, 10, "New Independent Empire"),
                    new IndependentColonyAssignment(2, 10, "New Independent Empire")
                ]));
        }
    }
}
