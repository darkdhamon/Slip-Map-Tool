using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Layout;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class AliensPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedRaceInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/aliens?sectorId=7&raceId=1");

        var cut = Render<Aliens>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alien biology", cut.Markup);
            Assert.Contains("Aurelian", cut.Markup);
            Assert.Contains("Summary", cut.Markup);
            Assert.Contains("No summary yet.", cut.Markup);
            Assert.Contains("Homeworld: Helios", cut.Markup);
            Assert.Contains("Species baseline traits", cut.Markup);
            Assert.Contains("Empire modifiers", cut.Markup);
            Assert.Contains("Membership role", cut.Markup);
            Assert.Contains("Population", cut.Markup);
            Assert.Contains("Base 6, mod +2", cut.Markup);
            Assert.Contains("Hair type", cut.Markup);
            Assert.Contains("Striped/Banded", cut.Markup);
            Assert.Contains("Visual profile", cut.Markup);
            Assert.Contains("Species: Aurelian", cut.Markup);
            Assert.Contains("Style target: detailed science-fiction species concept art", cut.Markup);
            Assert.Contains("Aurelian import", cut.Markup);
            Assert.DoesNotContain("? null", cut.Markup);
        });
    }

    [Fact]
    public void FiltersRacesBySearchQueryAndClearsFilters()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalRaces:
            [
                CreateRace(2, "Krell", "Reptilian", "Arid")
            ]));

        var cut = Render<Aliens>();

        cut.Find("input[placeholder='Name, summary, or notes...']").Input("Krell");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Krell", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 race", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Clear filters")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".record-row").Count);
            Assert.Contains("Showing 2 races", cut.Markup);
        });
    }

    [Fact]
    public void SearchPanelStartsCollapsed()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var cut = Render<Aliens>();

        cut.WaitForAssertion(() =>
        {
            var searchPanel = cut.Find(".record-filter-panel");
            Assert.DoesNotContain("open", searchPanel.OuterHtml, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void FiltersRacesByEnvironmentWhenFilterIsApplied()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalRaces:
            [
                CreateRace(2, "Krell", "Reptilian", "Arid")
            ]));

        var cut = Render<Aliens>();

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Show filters")
            .Click();

        cut.Find("input[placeholder='All environments']").Input("Arid");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Krell", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 race", cut.Markup);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalRaces()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var additionalRaces = new List<AlienRace>();
        for (var index = 0; index < 121; index++)
        {
            additionalRaces.Add(CreateRace(index + 10, $"Race {index:D3}", "Humanoid", "Temperate"));
        }

        ConfigureServices(CreateContext(additionalRaces: additionalRaces));

        var cut = Render<Aliens>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 30 races+", cut.Markup);
            Assert.DoesNotContain("Race 030", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Load more")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 60 races+", cut.Markup);
            Assert.Contains("Race 030", cut.Markup);
        });
    }

    [Fact]
    public void ShowsSearchingStateWhileFilteredRaceLookupLoadsAdditionalPages()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var additionalRaces = new List<AlienRace> { CreateRace(999, "Zeta Melolonian", "Insectoid", "Temperate") };

        var context = CreateContext(additionalRaces: additionalRaces);
        ConfigureServices(context, queryService: new DelayedAlienRaceQueryService(context, TimeSpan.FromMilliseconds(250)));

        var cut = Render<Aliens>();

        cut.Find("input[placeholder='Name, summary, or notes...']").Input("Melolonian");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Searching races", cut.Markup);
            Assert.Contains("Looking for race matches in the database", cut.Markup);
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Zeta Melolonian", cut.Markup);
            Assert.DoesNotContain("Searching races", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FiltersRacesByTotalCostAndTechLevelsThroughQueryService()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var context = CreateContext(
            additionalRaces:
            [
                CreateRace(2, "Krell", "Reptilian", "Arid"),
                CreateRace(3, "Meloloian", "Insectoid", "Temperate")
            ]);

        context.Empires[0].CivilizationProfile.TechLevel = 6;
        context.Empires[0].RaceMemberships.Clear();
        context.Empires[0].RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = 1,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 3200
        });
        context = context with
        {
            Empires =
            [
                context.Empires[0],
                new Empire
                {
                    Id = 3,
                    Name = "Krell Reach",
                    GovernmentType = "Council",
                    CivilizationProfile = new CivilizationProfile { TechLevel = 8 },
                    RaceMemberships =
                    {
                        new EmpireRaceMembership
                        {
                            RaceId = 2,
                            IsPrimary = true,
                            Role = EmpireRaceRole.Member,
                            PopulationMillions = 200
                        }
                    }
                }
            ]
        };

        ConfigureServices(context);

        var cut = Render<Aliens>();

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Show filters")
            .Click();

        cut.Find("input[placeholder='Any StarWin TL']").Input("8");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Krell", visibleRows[0].TextContent);
        });

        cut.Find("input[placeholder='Any point cost']").Input("0");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Krell", visibleRows[0].TextContent);
        });
    }

    [Fact]
    public void NavigatesToRelatedExplorerPagesFromAlienActions()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/aliens?sectorId=7&raceId=1");

        var cut = Render<Aliens>();

        cut.WaitForAssertion(() => Assert.Contains("Homeworld: Helios", cut.Markup));

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Homeworld: Helios")
            .Click();

        Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101&raceId=1", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll(".relationship-row")
            .Single(button => button.TextContent.Contains("Orion Compact", StringComparison.Ordinal)
                && button.TextContent.Contains("3200 million", StringComparison.Ordinal))
            .Click();

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Open empire")
            .Click();

        Assert.EndsWith("/sector-explorer/empires?sectorId=7&systemId=11&raceId=1&empireId=2", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void OpensRaceImageInPreviewModal()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(), images:
        [
            new EntityImage
            {
                Id = 1,
                TargetKind = EntityImageTargetKind.AlienRace,
                TargetId = 1,
                FileName = "aurelian.png",
                RelativePath = "/uploads/aurelian.png",
                IsPrimary = true
            }
        ]);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/aliens?sectorId=7&raceId=1");

        var cut = Render<Aliens>();

        cut.WaitForAssertion(() => Assert.Contains("aurelian.png", cut.Markup));

        cut.Find(".entity-image-trigger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Race image preview", cut.Markup);
            Assert.Contains("image-preview-full", cut.Markup);
            Assert.Contains("/uploads/aurelian.png", cut.Markup);
        });
    }

    [Fact]
    public void ShowOnlyChangedFilterRetainsControlsWhenResultsAreEmpty()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var context = CreateContext();
        var empire = Assert.Single(context.Empires);
        empire.CivilizationModifiers.Militancy = 0;
        empire.CivilizationModifiers.Art = 0;
        empire.CivilizationProfile.Militancy = context.AlienRaces.Single(race => race.Id == 1).CivilizationProfile.Militancy;
        empire.CivilizationProfile.Art = context.AlienRaces.Single(race => race.Id == 1).CivilizationProfile.Art;

        ConfigureServices(context);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/aliens?sectorId=7&raceId=1");

        var cut = Render<Aliens>();

        cut.WaitForAssertion(() => Assert.Contains("Only changed empires", cut.Markup));

        cut.Find("input[type='checkbox']").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No empire-specific civilization modifiers match the current filters.", cut.Markup);
            Assert.Contains("Only changed empires", cut.Markup);
            Assert.Contains("Search empires", cut.Markup);
        });
    }

    private void ConfigureServices(
        StarWinExplorerContext context,
        IReadOnlyList<EntityImage>? images = null,
        IStarWinExplorerQueryService? queryService = null)
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton(queryService ?? new FakeExplorerQueryService(context));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService(images ?? []));
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService());
    }

    private static StarWinExplorerContext CreateContext(
        IEnumerable<AlienRace>? additionalRaces = null)
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
            ColonistRaceName = "Aurelian",
            ControllingEmpireId = 2,
            FoundingEmpireId = 2,
            AllegianceId = 2,
            AllegianceName = "Orion Compact"
        };
        world.Colony.Demographics.Add(new ColonyDemographic
        {
            RaceId = 1,
            RaceName = "Aurelian",
            PopulationPercent = 92
        });

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

        var primaryRace = CreateRace(1, "Aurelian", "Humanoid", "Temperate");
        primaryRace.HomePlanetId = world.Id;

        var empire = new Empire
        {
            Id = 2,
            Name = "Orion Compact",
            GovernmentType = "Council",
            Planets = 4,
            NativePopulationMillions = 3200
        };
        empire.CivilizationProfile.TechLevel = 9;
        empire.CivilizationProfile.SpatialAge = 6;
        empire.CivilizationModifiers.Militancy = 2;
        empire.CivilizationModifiers.Art = -1;
        empire.Religions.Add(new EmpireReligion
        {
            EmpireId = empire.Id,
            ReligionId = 1,
            ReligionName = "Aurelian Faith",
            PopulationPercent = 100m
        });
        empire.Founding.FoundingWorldId = world.Id;
        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = primaryRace.Id,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 3200
        });

        var races = new List<AlienRace> { primaryRace };
        if (additionalRaces is not null)
        {
            foreach (var additionalRace in additionalRaces)
            {
                additionalRace.HomePlanetId = world.Id;
                races.Add(additionalRace);
                world.Colony.Demographics.Add(new ColonyDemographic
                {
                    RaceId = additionalRace.Id,
                    RaceName = additionalRace.Name,
                    PopulationPercent = 1
                });

                if (additionalRace.DevotionLevel != AlienDevotionLevel.None)
                {
                    empire.RaceMemberships.Add(new EmpireRaceMembership
                    {
                        RaceId = additionalRace.Id,
                        IsPrimary = false,
                        Role = EmpireRaceRole.Member,
                        PopulationMillions = 25
                    });
                }
            }
        }

        return new StarWinExplorerContext([sector], sector, races, [empire], []);
    }

    private static AlienRace CreateRace(int raceId, string name, string appearanceType, string environmentType, bool addMembership = true)
    {
        var race = new AlienRace
        {
            Id = raceId,
            Name = name,
            AppearanceType = appearanceType,
            EnvironmentType = environmentType,
            BodyChemistry = "Carbon",
            BodyCoverType = "Feathered",
            Diet = "Omnivore",
            Reproduction = "Sexual",
            ReproductionMethod = "Live birth",
            DevotionLevel = AlienDevotionLevel.High,
            MassKg = 85,
            SizeCm = 180,
            LimbPairCount = 2,
            HairType = "Rare",
            ColorPattern = "Striped/Banded",
            ImportDataJson = "{ \"legacy\": \"Aurelian import\" }"
        };
        race.CivilizationProfile.Militancy = 6;
        race.CivilizationProfile.Determination = 4;
        race.CivilizationProfile.RacialTolerance = 5;
        race.CivilizationProfile.Progressiveness = 7;
        race.CivilizationProfile.Loyalty = 8;
        race.CivilizationProfile.SocialCohesion = 6;
        race.CivilizationProfile.Art = 3;
        race.CivilizationProfile.Individualism = 5;
        race.BiologyProfile.Lifespan = 120;
        race.BiologyProfile.Body = 1;
        race.BiologyProfile.Mind = 2;
        race.BiologyProfile.Speed = 1;
        race.BiologyProfile.PsiRating = 0;
        race.HomePlanetId = 101;
        race.LimbTypes = ["Pair 1: Arms", "Pair 2: Legs"];
        race.Colors = ["Gold", "Blue"];
        race.HairColors = ["Black"];
        race.EyeColors = ["Green"];
        race.EyeCharacteristics = ["Single"];
        race.Abilities = ["Acute hearing"];
        race.BodyCharacteristics = ["Tail"];

        if (!addMembership)
        {
            race.DevotionLevel = AlienDevotionLevel.None;
        }

        return race;
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

    private class FakeExplorerQueryService(StarWinExplorerContext context) : IStarWinExplorerQueryService
    {
        public Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            var races = GetSectorRaces(sectorId);
            var superscienceRaceIds = GetSuperscienceRaceIds(sectorId);
            var empires = GetSectorEmpires(sectorId);

            return Task.FromResult(new ExplorerAlienRaceFilterOptions(
                races.Select(race => race.EnvironmentType).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().OrderBy(value => value).ToList(),
                races.Select(race => race.AppearanceType).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().OrderBy(value => value).ToList(),
                empires.Select(empire => (int)empire.CivilizationProfile.TechLevel).Distinct().OrderBy(value => value).ToList(),
                empires.SelectMany(empire => empire.RaceMemberships
                        .Where(membership => races.Any(race => race.Id == membership.RaceId))
                        .Select(membership => GurpsTechnologyLevelMapper.FormatDisplay(
                            empire.CivilizationProfile.TechLevel,
                            superscienceRaceIds.Contains(membership.RaceId))))
                    .Distinct()
                    .OrderBy(value => value)
                    .ToList()));
        }

        public virtual Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
        {
            var items = GetSectorRaces(request.SectorId)
                .Where(race => MatchesRaceFilters(request, race))
                .OrderBy(race => race.Name)
                .ThenBy(race => race.Id)
                .Select(race => new ExplorerAlienRaceListItem(race.Id, race.Name, race.AppearanceType, race.EnvironmentType))
                .ToList();

            var page = items.Skip(request.Offset).Take(request.Limit + 1).ToList();
            var hasMore = page.Count > request.Limit;
            return Task.FromResult(new ExplorerAlienRaceListPage(page.Take(request.Limit).ToList(), hasMore));
        }

        public virtual Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
        {
            var race = GetSectorRaces(sectorId).FirstOrDefault(item => item.Id == raceId);
            return Task.FromResult(race is null
                ? null
                : new ExplorerAlienRaceListItem(race.Id, race.Name, race.AppearanceType, race.EnvironmentType));
        }

        public virtual Task<ExplorerAlienRaceDetail?> LoadAlienRaceDetailAsync(int sectorId, int raceId, CancellationToken cancellationToken = default)
        {
            var sector = context.Sectors.FirstOrDefault(item => item.Id == sectorId);
            var race = GetSectorRaces(sectorId).FirstOrDefault(item => item.Id == raceId);
            if (sector is null || race is null)
            {
                return Task.FromResult<ExplorerAlienRaceDetail?>(null);
            }

            var homeWorld = sector.Systems
                .SelectMany(system => system.Worlds)
                .FirstOrDefault(world => world.Id == race.HomePlanetId);
            var empires = context.Empires
                .Where(empire => empire.RaceMemberships.Any(membership => membership.RaceId == raceId))
                .ToList();

            return Task.FromResult<ExplorerAlienRaceDetail?>(new ExplorerAlienRaceDetail(sectorId, race, homeWorld, empires));
        }

        public Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        private IReadOnlyList<AlienRace> GetSectorRaces(int sectorId)
        {
            var sector = context.Sectors.FirstOrDefault(item => item.Id == sectorId);
            if (sector is null)
            {
                return [];
            }

            var raceIds = sector.Systems
                .SelectMany(system => system.Worlds)
                .SelectMany(world => new[]
                {
                    world.AlienRaceId,
                    world.Colony?.RaceId
                }.Where(id => id.HasValue).Select(id => id!.Value))
                .ToHashSet();

            foreach (var demographicRaceId in sector.Systems
                .SelectMany(system => system.Worlds)
                .Where(world => world.Colony is not null)
                .SelectMany(world => world.Colony!.Demographics)
                .Select(demographic => demographic.RaceId))
            {
                raceIds.Add(demographicRaceId);
            }

            return context.AlienRaces.Where(race => raceIds.Contains(race.Id)).ToList();
        }

        private IReadOnlyList<Empire> GetSectorEmpires(int sectorId)
        {
            var sectorRaceIds = GetSectorRaces(sectorId).Select(race => race.Id).ToHashSet();
            return context.Empires
                .Where(empire => empire.RaceMemberships.Any(membership => sectorRaceIds.Contains(membership.RaceId)))
                .ToList();
        }

        private HashSet<int> GetSuperscienceRaceIds(int sectorId)
        {
            var sector = context.Sectors.First(item => item.Id == sectorId);
            var homeSystems = sector.Systems
                .SelectMany(system => system.Worlds)
                .Where(world => world.AlienRaceId.HasValue)
                .ToDictionary(world => world.AlienRaceId!.Value, world => world.StarSystemId);

            return sector.Systems
                .SelectMany(system => system.Worlds)
                .Where(world => world.Colony is not null && homeSystems.TryGetValue(world.Colony.RaceId, out var homeSystemId) && homeSystemId != world.StarSystemId)
                .Select(world => world.Colony!.RaceId)
                .Select(raceId => (int)raceId)
                .ToHashSet();
        }

        private bool MatchesRaceFilters(ExplorerAlienRaceListPageRequest request, AlienRace race)
        {
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var query = request.Query.Trim();
                if (!race.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.EnvironmentType)
                && !string.Equals(race.EnvironmentType, request.EnvironmentType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.AppearanceType)
                && !string.Equals(race.AppearanceType, request.AppearanceType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var sectorEmpires = GetSectorEmpires(request.SectorId)
                .Where(empire => empire.RaceMemberships.Any(membership => membership.RaceId == race.Id))
                .ToList();

            if (request.StarWinTechLevel.HasValue
                && sectorEmpires.All(empire => empire.CivilizationProfile.TechLevel != request.StarWinTechLevel.Value))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.GurpsTechLevel)
                && GurpsTechnologyLevelMapper.TryParseDisplay(request.GurpsTechLevel, out var gurpsTechLevel, out _)
                && sectorEmpires.All(empire => GurpsTechnologyLevelMapper.GetBaseTechLevel(empire.CivilizationProfile.TechLevel) != gurpsTechLevel))
            {
                return false;
            }

            var requiresSuperscience = request.RequireSuperscience
                || (!string.IsNullOrWhiteSpace(request.GurpsTechLevel)
                    && GurpsTechnologyLevelMapper.TryParseDisplay(request.GurpsTechLevel, out _, out var gurpsSuperscience)
                    && gurpsSuperscience);
            if (requiresSuperscience && !GetSuperscienceRaceIds(request.SectorId).Contains(race.Id))
            {
                return false;
            }

            if (request.MaxTotalPointCost.HasValue && CalculateTotalPointCost(race, sectorEmpires.FirstOrDefault()) > request.MaxTotalPointCost.Value)
            {
                return false;
            }

            return true;
        }

        private static int CalculateTotalPointCost(AlienRace race, Empire? primaryEmpire)
        {
            var total = AttributeDelta(race.BiologyProfile.Body) * 10
                + AttributeDelta(race.BiologyProfile.Mind) * 20
                + AttributeDelta(race.BiologyProfile.Speed) * 20;

            if (race.MassKg >= 130)
            {
                total += 10;
            }
            else if (race.MassKg <= 50)
            {
                total -= 10;
            }

            if (race.BiologyProfile.Speed >= 12)
            {
                total += 5;
            }

            if (race.BiologyProfile.Lifespan >= 36)
            {
                total += 2;
            }

            if (race.BiologyProfile.PsiRating >= PsiPowerRating.Good)
            {
                total += Math.Max(1, (int)race.BiologyProfile.PsiPower) * 5;
            }

            if (race.EnvironmentType.Contains("Vacuum", StringComparison.OrdinalIgnoreCase))
            {
                total += 5;
            }

            if (race.EnvironmentType.Contains("Aquatic", StringComparison.OrdinalIgnoreCase))
            {
                total += 10;
                total += 1;
            }

            if (race.AppearanceType.Contains("Avian", StringComparison.OrdinalIgnoreCase))
            {
                total += 40;
            }

            if (race.BodyCoverType.Contains("Hard", StringComparison.OrdinalIgnoreCase)
                || race.BodyCoverType.Contains("Crystal", StringComparison.OrdinalIgnoreCase)
                || race.BodyCoverType.Contains("Scales", StringComparison.OrdinalIgnoreCase))
            {
                total += 5;
            }

            if (primaryEmpire?.CivilizationProfile.TechLevel >= 9)
            {
                total += 1;
            }

            if (race.EnvironmentType.Contains("Subterranean", StringComparison.OrdinalIgnoreCase))
            {
                total -= 10;
            }

            if (race.Diet.Contains("Mineral", StringComparison.OrdinalIgnoreCase)
                || race.Diet.Contains("Energy", StringComparison.OrdinalIgnoreCase))
            {
                total -= 10;
            }

            if (race.BiologyProfile.PsiRating is PsiPowerRating.VeryPoor or PsiPowerRating.Poor)
            {
                total -= 5;
            }

            if (race.EnvironmentType.Contains("Aerial", StringComparison.OrdinalIgnoreCase))
            {
                total += 2;
            }

            if (race.BiologyProfile.Mind >= 12)
            {
                total += 2;
            }

            return total;
        }

        private static int AttributeDelta(byte score)
        {
            return Math.Clamp((score - 10) / 2, -3, 3);
        }
    }

    private sealed class DelayedAlienRaceQueryService(StarWinExplorerContext context, TimeSpan delay) : FakeExplorerQueryService(context)
    {
        public override async Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(request.Query)
                || !string.IsNullOrWhiteSpace(request.EnvironmentType)
                || !string.IsNullOrWhiteSpace(request.AppearanceType))
            {
                await Task.Delay(delay, cancellationToken);
            }

            return await base.LoadAlienRaceListPageAsync(request, cancellationToken);
        }
    }

    private sealed class FakeSearchService : IStarWinSearchService
    {
        public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30)
        {
            return [];
        }
    }

    private sealed class FakeImageService(IReadOnlyList<EntityImage> images) : IStarWinImageService
    {
        public Task<IReadOnlyList<EntityImage>> GetImagesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(images);
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
