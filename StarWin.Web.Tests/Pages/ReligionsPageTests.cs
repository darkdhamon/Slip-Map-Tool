using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Layout;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class ReligionsPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedReligionInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(), CreateReligionDetails());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/religions?sectorId=7&religionId=10");

        var cut = Render<Religions>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Religion profile", cut.Markup);
            Assert.Contains("Solar Doctrine", cut.Markup);
            Assert.Contains("Church tradition tracked across 1 empire and 1 species in this sector (imported).", cut.Markup);
            Assert.Contains("75% declared", cut.Markup);
            Assert.Contains("Orion Compact", cut.Markup);
            Assert.Contains("Human", cut.Markup);
        });
    }

    [Fact]
    public void FiltersReligionsBySearchAndType()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(), CreateReligionDetails());

        var cut = Render<Religions>();

        cut.Find("input[placeholder='Name or type...']").Input("Dune");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Dune Path", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 religion", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Clear filters")
            .Click();

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Show filters")
            .Click();

        cut.Find("input[placeholder='All types']").Input("Church");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Solar Doctrine", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 religion", cut.Markup);
        });
    }

    [Fact]
    public void NavigatesToRelatedEmpireFromReligionDetail()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(), CreateReligionDetails());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/religions?sectorId=7&religionId=10");

        var cut = Render<Religions>();

        cut.WaitForAssertion(() => Assert.Contains("Orion Compact", cut.Markup));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Orion Compact", StringComparison.Ordinal))
            .Click();

        Assert.EndsWith("/sector-explorer/empires?sectorId=7&systemId=11&empireId=2&religionId=10", navigationManager.Uri, StringComparison.Ordinal);
    }

    private void ConfigureServices(StarWinExplorerContext context, IReadOnlyList<ExplorerReligionDetail> religionDetails)
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinExplorerQueryService>(new FakeExplorerQueryService(religionDetails));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
    }

    private static StarWinExplorerContext CreateContext()
    {
        var world = new World
        {
            Id = 101,
            Name = "Helios",
            WorldType = "Terran",
            StarSystemId = 11
        };

        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios"
        };
        system.Worlds.Add(world);

        var sector = new StarWinSector
        {
            Id = 7,
            Name = "Del Corra"
        };
        sector.Systems.Add(system);

        var races = new List<AlienRace>
        {
            new() { Id = 1, Name = "Human" },
            new() { Id = 2, Name = "Krell" }
        };

        var orionCompact = new Empire
        {
            Id = 2,
            Name = "Orion Compact"
        };
        orionCompact.CivilizationProfile.TechLevel = 9;
        orionCompact.Founding.FoundingWorldId = 101;

        var krellReach = new Empire
        {
            Id = 3,
            Name = "Krell Reach"
        };
        krellReach.CivilizationProfile.TechLevel = 8;
        krellReach.Founding.FoundingWorldId = 101;

        return new StarWinExplorerContext([sector], sector, races, [orionCompact, krellReach]);
    }

    private static IReadOnlyList<ExplorerReligionDetail> CreateReligionDetails()
    {
        var solarDoctrine = new Religion
        {
            Id = 10,
            Name = "Solar Doctrine",
            Type = "Church"
        };

        var dunePath = new Religion
        {
            Id = 11,
            Name = "Dune Path",
            Type = "Animism",
            IsUserDefined = true
        };

        return
        [
            new ExplorerReligionDetail(
                7,
                solarDoctrine,
                [
                    new ExplorerReligionEmpireListing(2, "Orion Compact", 75m, 3200, 9)
                ],
                [
                    new ExplorerReligionRaceListing(1, "Human", 1, 3200)
                ]),
            new ExplorerReligionDetail(
                7,
                dunePath,
                [
                    new ExplorerReligionEmpireListing(3, "Krell Reach", 60m, 900, 8)
                ],
                [
                    new ExplorerReligionRaceListing(2, "Krell", 1, 900)
                ])
        ];
    }

    private sealed class FakeExplorerContextService(StarWinExplorerContext context) : IStarWinExplorerContextService
    {
        public Task<StarWinExplorerContext> LoadShellAsync(int? preferredSectorId = null, bool includeReferenceData = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context);
        }
    }

    private sealed class FakeExplorerQueryService(IReadOnlyList<ExplorerReligionDetail> religionDetails) : IStarWinExplorerQueryService
    {
        public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
            return Task.FromResult(new ExplorerReligionFilterOptions(
                religionDetails.Select(detail => detail.Religion.Type)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList()));
        }

        public Task<ExplorerReligionListPage> LoadReligionListPageAsync(ExplorerReligionListPageRequest request, CancellationToken cancellationToken = default)
        {
            var items = religionDetails
                .Where(detail => Matches(detail, request))
                .OrderBy(detail => detail.Religion.Name)
                .ThenBy(detail => detail.Religion.Id)
                .Select(detail => new ExplorerReligionListItem(
                    detail.Religion.Id,
                    detail.Religion.Name,
                    detail.Religion.Type,
                    detail.Empires.Count))
                .ToList();

            var page = items.Skip(request.Offset).Take(request.Limit + 1).ToList();
            var hasMore = page.Count > request.Limit;
            return Task.FromResult(new ExplorerReligionListPage(page.Take(request.Limit).ToList(), hasMore));
        }

        public Task<ExplorerReligionListItem?> LoadReligionListItemAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
        {
            var detail = religionDetails.FirstOrDefault(item => item.Religion.Id == religionId);
            return Task.FromResult(detail is null
                ? null
                : new ExplorerReligionListItem(
                    detail.Religion.Id,
                    detail.Religion.Name,
                    detail.Religion.Type,
                    detail.Empires.Count));
        }

        public Task<ExplorerReligionDetail?> LoadReligionDetailAsync(int sectorId, int religionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(religionDetails.FirstOrDefault(item => item.Religion.Id == religionId));
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

        private static bool Matches(ExplorerReligionDetail detail, ExplorerReligionListPageRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var query = request.Query.Trim();
                if (!detail.Religion.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    && !detail.Religion.Type.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return string.IsNullOrWhiteSpace(request.Type)
                || string.Equals(detail.Religion.Type, request.Type.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class FakeSearchService : IStarWinSearchService
    {
        public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30) => [];
    }
}
