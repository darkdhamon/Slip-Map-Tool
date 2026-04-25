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

    private sealed class FakeExplorerQueryService : IStarWinExplorerQueryService
    {
        public int LoadTimelineEventTypesCallCount { get; private set; }

        public List<ExplorerTimelinePageRequest> PageRequests { get; } = [];

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
            return Task.FromResult<ExplorerTimelineEventDetail?>(null);
        }
    }
}
