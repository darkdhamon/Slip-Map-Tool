using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
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

    }

    private void ConfigureServices(StarWinSector sector, FakeWorkspace workspace)
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinWorkspace>(workspace);
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(sector));
        Services.AddSingleton<IStarWinExplorerQueryService>(new FakeExplorerQueryService(sector));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
    }

    private static StarWinSector CreateSector()
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
        sector.SavedRoutes.Add(new SectorSavedRoute
        {
            Id = 1,
            SectorId = 7,
            SourceSystemId = 11,
            TargetSystemId = 11,
            DistanceParsecs = 0m,
            TravelTimeYears = 0m,
            TechnologyLevel = 6,
            TierName = "Basic Hyperlane"
        });

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

        public IReadOnlyList<EmpireContact> EmpireContacts => [];

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
                [new Empire { Id = 8, Name = "Orion Compact" }],
                []);
        }

        public Task<StarWinExplorerContext> LoadShellAsync(bool includeSavedRoutes = true, bool includeReferenceData = true, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context);
        }

        public Task<StarWinSector?> LoadSectorAsync(int sectorId, ExplorerSectorLoadSections loadSections, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StarWinSector?>(context.Sectors.FirstOrDefault(sector => sector.Id == sectorId));
        }
    }

    private sealed class FakeExplorerQueryService : IStarWinExplorerQueryService
    {
        private readonly ExplorerSectorOverviewData overviewData;

        public FakeExplorerQueryService(StarWinSector sector)
        {
            overviewData = new ExplorerSectorOverviewData(
                sector.Id,
                sector.Systems.Count,
                sector.Systems.SelectMany(system => system.Worlds).Count(),
                sector.Systems.SelectMany(system => system.Worlds).Count(world => world.Colony is not null),
                1,
                1,
                [new ExplorerLookupOption(11, "Helios")],
                [new ExplorerLookupOption(8, "Orion Compact")]);
        }

        public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(overviewData);
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
