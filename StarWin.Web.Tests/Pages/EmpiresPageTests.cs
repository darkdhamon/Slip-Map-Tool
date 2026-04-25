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

public sealed class EmpiresPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedEmpireInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(CreateContext()));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Empire profile", cut.Markup);
            Assert.Contains("Orion Compact", cut.Markup);
            Assert.Contains("Homeworld: Helios", cut.Markup);
            Assert.Contains("href=\"/sector-explorer/worlds?sectorId=7\"", cut.Markup);
        });
    }

    private static StarWinExplorerContext CreateContext()
    {
        var world = new World
        {
            Id = 101,
            Name = "Helios",
            WorldType = "Terran",
            AtmosphereType = "Breathable",
            StarSystemId = 11
        };

        world.Colony = new Colony
        {
            Id = 201,
            WorldId = world.Id,
            Name = "Helios Prime",
            ColonyClass = "Capital",
            EstimatedPopulation = 3_200_000_000,
            RaceId = 1,
            ColonistRaceName = "Human",
            ControllingEmpireId = 2,
            FoundingEmpireId = 2,
            AllegianceId = 2,
            AllegianceName = "Orion Compact"
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

        var race = new AlienRace
        {
            Id = 1,
            Name = "Human"
        };

        var empire = new Empire
        {
            Id = 2,
            Name = "Orion Compact",
            Planets = 4,
            MilitaryPower = 9,
            NativePopulationMillions = 3200
        };
        empire.CivilizationProfile.TechLevel = 9;
        empire.Founding.FoundingWorldId = world.Id;
        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = race.Id,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 3200
        });

        return new StarWinExplorerContext([sector], sector, [race], [empire], []);
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
        public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30)
        {
            return [];
        }
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
        public Task<string> SaveNameAsync(EntityNoteTargetKind targetKind, int targetId, string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(name);
        }
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
}
