using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Tests.Components;

public sealed class ExplorerTimelineSectionTests : BunitContext
{
    [Fact]
    public void LoadsEventTypesAndReloadsWhenTypeChanges()
    {
        var queryService = new FakeExplorerQueryService();
        Services.AddSingleton<IStarWinExplorerQueryService>(queryService);

        var cut = Render<ExplorerTimelineSection>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SectorName, "Del Corra"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, queryService.LoadTimelineEventTypesCallCount);
            Assert.Single(queryService.PageRequests);
            Assert.Contains("All event types", cut.Markup);
            Assert.Contains("<option value=\"War\">War</option>", cut.Markup);
        });

        var eventTypeFilter = cut.Find("select");
        eventTypeFilter.Change("War");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, queryService.PageRequests.Count);
            Assert.Equal("War", queryService.PageRequests[^1].EventType);
            Assert.Contains("for War", cut.Markup);
        });
    }

    [Fact]
    public void TogglesImportDataForSelectedEvent()
    {
        var queryService = new FakeExplorerQueryService
        {
            TimelineDetail = new ExplorerTimelineEventDetail(
                1,
                "Border war begins",
                "War",
                "Century 1",
                1,
                "Border war begins",
                "{\n  \"Century\": \"1\",\n  \"Type\": \"War\"\n}",
                null,
                null,
                null,
                null,
                null,
                null)
        };

        Services.AddSingleton<IStarWinExplorerQueryService>(queryService);

        var cut = Render<ExplorerTimelineSection>(parameters => parameters
            .Add(component => component.SectorId, 7)
            .Add(component => component.SectorName, "Del Corra"));

        cut.WaitForAssertion(() => Assert.Single(queryService.PageRequests));

        cut.Find(".timeline-event-card-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Show Import Data", cut.Markup);
            Assert.DoesNotContain("\"Century\": \"1\"", cut.Markup);
        });

        cut.Find(".timeline-secondary-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Hide Import Data", cut.Markup);
            Assert.Contains("\"Century\": \"1\"", cut.Markup);
        });

        cut.Find(".timeline-secondary-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Show Import Data", cut.Markup);
            Assert.DoesNotContain("\"Century\": \"1\"", cut.Markup);
        });
    }

    private sealed class FakeExplorerQueryService : IStarWinExplorerQueryService
    {
        public int LoadTimelineEventTypesCallCount { get; private set; }

        public List<ExplorerTimelinePageRequest> PageRequests { get; } = [];

        public ExplorerTimelineEventDetail? TimelineDetail { get; set; }

        public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExplorerSectorOverviewData(sectorId, 0, 0, 0, 0, 0, [], []));
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
            LoadTimelineEventTypesCallCount++;
            return Task.FromResult<IReadOnlyList<string>>(["Diplomacy", "War"]);
        }

        public Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
        {
            PageRequests.Add(request);

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

            return Task.FromResult(new ExplorerTimelinePage(items, false));
        }

        public Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TimelineDetail);
        }
    }
}
