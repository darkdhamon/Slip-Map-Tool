using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class TimelinePageTests : BunitContext
{
    [Fact]
    public void RendersDedicatedTimelinePageAndReloadsByEventType()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var queryService = new FakeExplorerQueryService();
        ConfigureServices(CreateContext(), queryService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/timeline?sectorId=7&systemId=11");

        var cut = Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sector chronology", cut.Markup);
            Assert.Contains("Border war begins", cut.Markup);
            Assert.Equal(1, queryService.LoadTimelineEventTypesCallCount);
            Assert.Single(queryService.PageRequests);
        });

        cut.Find(".timeline-filter-panel select").Change("War");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, queryService.PageRequests.Count);
            Assert.Equal("War", queryService.PageRequests[^1].EventType);
            Assert.Contains("for War", cut.Markup);
        });
    }

    [Fact]
    public void TimelineLinksNavigateToExtractedExplorerPages()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var queryService = new FakeExplorerQueryService
        {
            TimelineDetail = new ExplorerTimelineEventDetail(
                1,
                "Border war begins",
                "War",
                "Century 1",
                1,
                "Border war begins",
                null,
                new ExplorerLookupOption(3, "Krell"),
                null,
                new ExplorerLookupOption(8, "Orion Compact"),
                new ExplorerLookupOption(501, "Helios Prime"),
                new ExplorerLookupOption(101, "Eos"),
                new ExplorerLookupOption(11, "Helios"))
        };
        ConfigureServices(CreateContext(), queryService);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/timeline?sectorId=7&systemId=11");

        var cut = Render<Timeline>();
        cut.WaitForAssertion(() => Assert.Contains("Border war begins", cut.Markup));

        cut.Find(".timeline-event-card-button").Click();
        cut.WaitForAssertion(() => Assert.Contains("Race: Krell", cut.Markup));

        cut.FindAll(".timeline-links button").Single(button => button.TextContent.Contains("Race: Krell", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/aliens?sectorId=7&systemId=11&raceId=3", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll(".timeline-links button").Single(button => button.TextContent.Contains("Empire: Orion Compact", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/empires?sectorId=7&systemId=11&empireId=8", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll(".timeline-links button").Single(button => button.TextContent.Contains("Colony: Helios Prime", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/colonies?sectorId=7&systemId=11&worldId=101&colonyId=501", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll(".timeline-links button").Single(button => button.TextContent.Contains("World: Eos", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll(".timeline-links button").Single(button => button.TextContent.Contains("System: Helios", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/sector-explorer/systems?sectorId=7&systemId=11", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void TogglesImportDataForSelectedTimelineEvent()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var queryService = new FakeExplorerQueryService
        {
            TimelineDetail = new ExplorerTimelineEventDetail(
                1,
                "Border war begins",
                "War",
                "Century 1",
                1,
                "Border war begins",
                "{\n  \"Century\": \"1\"\n}",
                null,
                null,
                null,
                null,
                null,
                null)
        };
        ConfigureServices(CreateContext(), queryService);

        var cut = Render<Timeline>();
        cut.WaitForAssertion(() => Assert.Contains("Border war begins", cut.Markup));

        cut.Find(".timeline-event-card-button").Click();
        cut.WaitForAssertion(() => Assert.Contains("Show Import Data", cut.Markup));

        cut.Find(".timeline-secondary-button").Click();
        cut.WaitForAssertion(() => Assert.Contains("\"Century\": \"1\"", cut.Markup));
    }

    [Fact]
    public void KeepsTimelineModalVisibleUntilInitialTimelineBatchLoads()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var queryService = new FakeExplorerQueryService
        {
            DelayFirstTimelinePage = true
        };
        ConfigureServices(CreateContext(), queryService);

        var cut = Render<Timeline>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Loading Timeline", cut.Markup);
            Assert.Contains("Preparing timeline events for the selected sector.", cut.Markup);
            Assert.Contains("Elapsed time:", cut.Markup);
        });

        queryService.ReleaseFirstTimelinePage();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Preparing timeline events for the selected sector.", cut.Markup);
            Assert.Contains("Border war begins", cut.Markup);
        });
    }

    private void ConfigureServices(StarWinExplorerContext context, FakeExplorerQueryService? queryService = null)
    {
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinExplorerQueryService>(queryService ?? new FakeExplorerQueryService());
    }

    private static StarWinExplorerContext CreateContext()
    {
        var world = new World
        {
            Id = 101,
            Name = "Eos",
            StarSystemId = 11
        };
        world.Colony = new Colony
        {
            Id = 501,
            WorldId = 101,
            Name = "Helios Prime",
            RaceId = 3,
            AllegianceId = 8
        };
        world.Colony.Demographics.Add(new ColonyDemographic
        {
            RaceId = 3,
            RaceName = "Krell",
            PopulationPercent = 100
        });

        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios",
            AllegianceId = 8
        };
        system.Worlds.Add(world);

        var sector = new StarWinSector { Id = 7, Name = "Del Corra" };
        sector.Systems.Add(system);

        return new StarWinExplorerContext(
            [sector],
            sector,
            [new AlienRace { Id = 3, Name = "Krell" }],
            [new Empire { Id = 8, Name = "Orion Compact" }],
            []);
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

    private sealed class FakeExplorerQueryService : IStarWinExplorerQueryService
    {
        public int LoadTimelineEventTypesCallCount { get; private set; }

        public List<ExplorerTimelinePageRequest> PageRequests { get; } = [];

        public ExplorerTimelineEventDetail? TimelineDetail { get; set; }
        public bool DelayFirstTimelinePage { get; set; }
        private TaskCompletionSource<bool>? firstTimelinePageSource;

        public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerSectorOverviewData(sectorId, 0, 0, 0, 0, 0, [], []));
        }

        public Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            LoadTimelineEventTypesCallCount++;
            return Task.FromResult<IReadOnlyList<string>>(["Diplomacy", "War"]);
        }

        public Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
        {
            PageRequests.Add(request);
            if (DelayFirstTimelinePage && request.Offset == 0)
            {
                firstTimelinePageSource ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return WaitForFirstTimelinePageAsync(request);
            }

            return Task.FromResult(CreateTimelinePage(request));
        }

        public void ReleaseFirstTimelinePage()
        {
            firstTimelinePageSource?.TrySetResult(true);
        }

        private async Task<ExplorerTimelinePage> WaitForFirstTimelinePageAsync(ExplorerTimelinePageRequest request)
        {
            if (firstTimelinePageSource is not null)
            {
                await firstTimelinePageSource.Task;
            }

            DelayFirstTimelinePage = false;
            return CreateTimelinePage(request);
        }

        private static ExplorerTimelinePage CreateTimelinePage(ExplorerTimelinePageRequest request)
        {
            IReadOnlyList<ExplorerTimelineListItem> items =
            [
                new ExplorerTimelineListItem(
                    1,
                    "Border war begins",
                    request.EventType ?? "War",
                    "Century 1",
                    1,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)
            ];

            return new ExplorerTimelinePage(items, false);
        }

        public Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TimelineDetail);
        }
    }
}
