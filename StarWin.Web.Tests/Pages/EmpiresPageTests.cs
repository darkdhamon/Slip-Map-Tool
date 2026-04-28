using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Layout;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class EmpiresPageTests : BunitContext
{
    [Fact]
    public void RendersRequestedEmpireInDedicatedPage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Empire profile", cut.Markup);
            Assert.Contains("Orion Compact", cut.Markup);
            Assert.Contains("Summary", cut.Markup);
            Assert.Contains("No summary yet.", cut.Markup);
            Assert.Contains("Homeworld: Helios", cut.Markup);
        });
    }

    [Fact]
    public void FiltersEmpiresBySummarySearchAndClearsFilters()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            primaryEmpireName: "Orion Compact",
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Zephyr League",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 2,
                    nativePopulationMillions: 800)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Zephyria",
                    colonyId: 202,
                    colonyName: "Zephyria Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]),
            new Dictionary<(EntityNoteTargetKind, int), EntityNote>
            {
                [(EntityNoteTargetKind.EmpireSummary, 3)] = new()
                {
                    TargetKind = EntityNoteTargetKind.EmpireSummary,
                    TargetId = 3,
                    Markdown = "Frontier trade coalition"
                }
            });

        var cut = Render<Empires>();

        cut.Find("input[placeholder='Name, summary, or notes...']").Input("Frontier");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Zephyr League", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 empire", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Clear filters")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".record-row").Count);
            Assert.Contains("Showing 2 empires", cut.Markup);
        });
    }

    [Fact]
    public void FiltersEmpiresByRaceWhenRaceFilterIsApplied()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalRaces:
            [
                new AlienRace
                {
                    Id = 2,
                    Name = "Krell"
                }
            ],
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Krell Dominion",
                    foundingWorldId: 102,
                    primaryRaceId: 2,
                    planets: 5,
                    nativePopulationMillions: 2100)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Kharon",
                    colonyId: 202,
                    colonyName: "Kharon Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]));

        var cut = Render<Empires>();

        cut.Find("input[placeholder='All races']").Input("2 - Krell");

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Krell Dominion", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 empire", cut.Markup);
        });
    }

    [Fact]
    public void LoadMoreRevealsAdditionalEmpires()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var additionalEmpires = new List<Empire>();
        var additionalWorlds = new List<World>();

        for (var index = 0; index < 30; index++)
        {
            var empireId = index + 10;
            var worldId = 200 + index;
            additionalEmpires.Add(CreateEmpire(
                empireId,
                $"Empire {index:D3}",
                foundingWorldId: worldId,
                primaryRaceId: 1,
                planets: 1,
                nativePopulationMillions: 10));
            additionalWorlds.Add(CreateWorld(
                worldId,
                $"World {index:D3}",
                colonyId: 500 + index,
                colonyName: $"Colony {index:D3}",
                controllingEmpireId: empireId,
                foundingEmpireId: empireId));
        }

        ConfigureServices(CreateContext(
            additionalEmpires: additionalEmpires,
            additionalWorlds: additionalWorlds));

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 30 empires+", cut.Markup);
            Assert.DoesNotContain("Orion Compact", cut.Markup);
        });

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Load more")
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Showing 31 empires", cut.Markup);
            Assert.Contains("Orion Compact", cut.Markup);
        });
    }

    [Fact]
    public void NavigatesToRelatedExplorerPagesFromEmpireActions()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() => Assert.Contains("Homeworld: Helios", cut.Markup));

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Homeworld: Helios")
            .Click();

        Assert.EndsWith("/sector-explorer/worlds?sectorId=7&systemId=11&worldId=101&empireId=2", navigationManager.Uri, StringComparison.Ordinal);

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Capital: Helios")
            .Click();

        Assert.EndsWith("/sector-explorer/colonies?sectorId=7&systemId=11&colonyId=201&empireId=2", navigationManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersEmpireWideRacePercentagesAndSearchableColonyBrowser()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalRaces:
            [
                new AlienRace
                {
                    Id = 2,
                    Name = "Krell"
                }
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Watchers Reach",
                    colonyId: 202,
                    colonyName: "Watcher Hold",
                    controllingEmpireId: 2,
                    foundingEmpireId: 7,
                    colonyRaceId: 2,
                    colonyRaceName: "Krell",
                    estimatedPopulation: 400_000_000)
            ]));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            var browserCards = cut.FindAll(".empire-browser-card");
            Assert.Equal(3, browserCards.Count);

            var memberRaceCard = browserCards.Single(card => card.TextContent.Contains("Member races"));
            var colonyCard = browserCards.Single(card => card.TextContent.Contains("Colonies"));

            Assert.Contains("2 tracked", memberRaceCard.TextContent);
            Assert.Contains("Krell", memberRaceCard.TextContent);
            Assert.Contains("11.1% of empire population", memberRaceCard.TextContent);
            Assert.Contains("2 total", colonyCard.TextContent);
        });

        cut.Find("input[placeholder='Colony, world, or system...']").Input("Watcher");

        cut.WaitForAssertion(() =>
        {
            var colonyCard = cut.FindAll(".empire-browser-card").Single(card => card.TextContent.Contains("Colonies"));
            Assert.Contains("1 of 2 shown", colonyCard.TextContent);
            Assert.Contains("Watcher Hold on Watchers Reach", colonyCard.TextContent);
            Assert.DoesNotContain("Helios Prime on Helios", colonyCard.TextContent);
        });
    }

    [Fact]
    public void OrdersMemberRacesByPopulationDescendingInEmpireView()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalRaces:
            [
                new AlienRace
                {
                    Id = 2,
                    Name = "Krell"
                }
            ],
            primaryWorld: CreateWorld(
                101,
                "Helios",
                colonyId: 201,
                colonyName: "Helios Prime",
                controllingEmpireId: 2,
                foundingEmpireId: 2,
                demographics:
                [
                    new ColonyDemographic
                    {
                        RaceId = 1,
                        RaceName = "Human",
                        Population = 100_000_000
                    },
                    new ColonyDemographic
                    {
                        RaceId = 2,
                        RaceName = "Krell",
                        Population = 400_000_000
                    }
                ])));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            var memberRaceCard = cut.FindAll(".empire-browser-card").Single(card => card.TextContent.Contains("Member races"));
            var memberRaceRows = memberRaceCard.QuerySelectorAll(".relationship-row").Take(2).ToList();

            Assert.Equal(2, memberRaceRows.Count);
            Assert.Contains("Krell", memberRaceRows[0].TextContent);
            Assert.Contains("Human", memberRaceRows[1].TextContent);
        });
    }

    [Fact]
    public void RendersEmpireRacialModifiersAgainstPrimaryRaceBaseline()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var context = CreateContext();
        var primaryRace = context.AlienRaces.Single(race => race.Id == 1);
        primaryRace.CivilizationProfile.Militancy = 11;
        primaryRace.CivilizationProfile.Determination = 12;
        primaryRace.CivilizationProfile.RacialTolerance = 9;
        primaryRace.CivilizationProfile.Progressiveness = 8;
        primaryRace.CivilizationProfile.Loyalty = 17;
        primaryRace.CivilizationProfile.SocialCohesion = 16;
        primaryRace.CivilizationProfile.Art = 10;
        primaryRace.CivilizationProfile.Individualism = 7;

        var empire = context.Empires.Single(item => item.Id == 2);
        empire.CivilizationProfile.Militancy = 13;
        empire.CivilizationProfile.Determination = 11;
        empire.CivilizationProfile.RacialTolerance = 9;
        empire.CivilizationProfile.Progressiveness = 8;
        empire.CivilizationProfile.Loyalty = 15;
        empire.CivilizationProfile.SocialCohesion = 16;
        empire.CivilizationProfile.Art = 10;
        empire.CivilizationProfile.Individualism = 7;
        empire.CivilizationModifiers.Militancy = 2;
        empire.CivilizationModifiers.Determination = -1;

        ConfigureServices(context);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            var modifierCard = cut.Find(".empire-modifier-card");
            Assert.Contains("Empire modifiers", modifierCard.TextContent);
            Assert.Contains("Militancy", modifierCard.TextContent);
            Assert.Contains("+2", modifierCard.TextContent);
            Assert.Contains("-1", modifierCard.TextContent);
            Assert.Contains("0", modifierCard.TextContent);
            Assert.DoesNotContain("Base 11", modifierCard.TextContent);
            Assert.DoesNotContain("Computed", modifierCard.TextContent);
        });
    }

    [Fact]
    public void ShowsTrackedWorldCountsAndHighlightsConqueredColonies()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Orion Compact",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 3,
                    nativePopulationMillions: 1600)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Watcher Rest",
                    colonyId: 202,
                    colonyName: "Watcher Hold",
                    controllingEmpireId: 3,
                    foundingEmpireId: 2)
            ]));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            var selectedRow = cut.FindAll(".record-row").Single(row => row.ClassList.Contains("active"));
            Assert.Contains("1 controlled / 2 tracked worlds", selectedRow.TextContent);

            var colonyCard = cut.FindAll(".empire-browser-card").Single(card => card.TextContent.Contains("Colonies"));
            Assert.Contains("1 controlled / 2 tracked", colonyCard.TextContent);

            var conqueredRow = colonyCard.QuerySelectorAll(".relationship-row.conquered").Single();
            Assert.Contains("Watcher Hold on Watcher Rest", conqueredRow.TextContent);
            Assert.Contains("controlled by Orion Compact", conqueredRow.TextContent);
        });
    }

    [Fact]
    public void RendersSearchableEmpireRelationships()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var context = CreateContext(
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Krell Reach",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 2,
                    nativePopulationMillions: 800),
                CreateEmpire(
                    4,
                    "Watcher Remnant",
                    foundingWorldId: 103,
                    primaryRaceId: 1,
                    planets: 1,
                    nativePopulationMillions: 300,
                    isFallen: true)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Krellos",
                    colonyId: 202,
                    colonyName: "Krell Hold",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3),
                CreateWorld(
                    103,
                    "Watcher Rest",
                    colonyId: 203,
                    colonyName: "Watcher Hold",
                    controllingEmpireId: 4,
                    foundingEmpireId: 4)
            ]);

        var empire = context.Empires.Single(item => item.Id == 2);
        empire.Contacts.Add(new EmpireContact { EmpireId = 2, OtherEmpireId = 3, Relation = "Trade", RelationCode = 2, Age = 4 });
        empire.Contacts.Add(new EmpireContact { EmpireId = 2, OtherEmpireId = 4, Relation = "War", RelationCode = 0, Age = 1 });

        ConfigureServices(context);

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            var relationshipCard = cut.FindAll(".empire-browser-card").Single(card => card.TextContent.Contains("Empire relationships"));
            Assert.Contains("2 tracked", relationshipCard.TextContent);
            Assert.Contains("Watcher Remnant", relationshipCard.TextContent);
            Assert.Contains("War; contact age 1", relationshipCard.TextContent);
            Assert.Contains("Krell Reach", relationshipCard.TextContent);
        });

        cut.Find("input[placeholder='Empire name...']").Input("Watcher");

        cut.WaitForAssertion(() =>
        {
            var relationshipCard = cut.FindAll(".empire-browser-card").Single(card => card.TextContent.Contains("Empire relationships"));
            Assert.Contains("1 of 2 shown", relationshipCard.TextContent);
            Assert.Contains("Watcher Remnant", relationshipCard.TextContent);
            Assert.DoesNotContain("Krell Reach", relationshipCard.TextContent);
        });

        cut.Find("input[placeholder='Empire name...']").Input(string.Empty);
        cut.FindAll("select").Single(select => select.TextContent.Contains("All relationship types")).Change("Trade");

        cut.WaitForAssertion(() =>
        {
            var relationshipCard = cut.FindAll(".empire-browser-card").Single(card => card.TextContent.Contains("Empire relationships"));
            Assert.Contains("1 of 2 shown", relationshipCard.TextContent);
            Assert.Contains("Krell Reach", relationshipCard.TextContent);
            Assert.DoesNotContain("Watcher Remnant", relationshipCard.TextContent);
        });
    }

    [Fact]
    public void RendersFallenEmpireStatusWhenEmpireHasNoControlledColonies()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            primaryEmpireName: "Ancient Watchers",
            primaryEmpireNativePopulationMillions: 950,
            primaryEmpirePlanets: 1,
            primaryEmpireIsFallen: true,
            primaryWorld: CreateWorld(
                101,
                "Helios",
                colonyId: 201,
                colonyName: "Helios Prime",
                controllingEmpireId: 3,
                foundingEmpireId: 2)));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Fallen empire", cut.Markup);
            Assert.Contains("This empire no longer controls any colonies in this sector.", cut.Markup);
            Assert.Contains("<dd>Fallen empire</dd>", cut.Markup);
        });
    }

    [Fact]
    public void FiltersEmpiresByFallenStatusWhenFilterIsApplied()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            primaryEmpireName: "Ancient Watchers",
            primaryEmpireNativePopulationMillions: 950,
            primaryEmpirePlanets: 1,
            primaryEmpireIsFallen: true,
            primaryWorld: CreateWorld(
                101,
                "Helios",
                colonyId: 201,
                colonyName: "Helios Prime",
                controllingEmpireId: 3,
                foundingEmpireId: 2),
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Orion Compact",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 3,
                    nativePopulationMillions: 1600)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Nova Helios",
                    colonyId: 202,
                    colonyName: "Nova Helios Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]));

        var cut = Render<Empires>();

        cut.Find("[data-testid='fallen-empire-filter-toggle']").Change(true);

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Single(visibleRows);
            Assert.Contains("Ancient Watchers", visibleRows[0].TextContent);
            Assert.Contains("Showing 1 empire", cut.Markup);
            Assert.DoesNotContain("Orion Compact", visibleRows[0].TextContent);
            Assert.NotNull(cut.Find("[data-testid='fallen-empire-filter-toggle']").GetAttribute("checked"));
        });
    }

    [Fact]
    public void RendersSciFiToggleVisualLayersForFallenEmpireFilter()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext());

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".record-filter-toggle-thumb-scan"));
            Assert.Equal(5, cut.FindAll(".record-filter-toggle-thumb-particle").Count);
            Assert.Equal(3, cut.FindAll(".record-filter-toggle-energy-ring").Count);
            Assert.NotNull(cut.Find(".record-filter-toggle-track-line"));
        });
    }

    [Fact]
    public void ShowsSearchingStateWhileEmpireFiltersReloadResults()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var context = CreateContext(
            primaryEmpireName: "Ancient Watchers",
            primaryEmpireNativePopulationMillions: 950,
            primaryEmpirePlanets: 1,
            primaryEmpireIsFallen: true,
            primaryWorld: CreateWorld(
                101,
                "Helios",
                colonyId: 201,
                colonyName: "Helios Prime",
                controllingEmpireId: 3,
                foundingEmpireId: 2),
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Orion Compact",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 3,
                    nativePopulationMillions: 1600)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Nova Helios",
                    colonyId: 202,
                    colonyName: "Nova Helios Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]);

        ConfigureServices(context, queryService: new DelayedEmpireQueryService(context, TimeSpan.FromMilliseconds(250)));

        var cut = Render<Empires>();

        cut.Find("[data-testid='fallen-empire-filter-toggle']").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Searching", cut.Markup);
            Assert.Contains("Searching empires with the current filters...", cut.Markup);
            Assert.Null(cut.Find("[data-testid='fallen-empire-filter-toggle']").GetAttribute("disabled"));
            Assert.NotNull(cut.Find("[data-testid='fallen-empire-filter-toggle']").GetAttribute("checked"));
            Assert.Null(cut.Find("input[placeholder='Name, summary, or notes...']").GetAttribute("disabled"));
            Assert.Null(cut.Find("input[placeholder='All races']").GetAttribute("disabled"));
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ancient Watchers", cut.Markup);
            Assert.DoesNotContain("Searching empires with the current filters...", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AllowsFallenToggleToBeClearedWhileResultsAreStillLoading()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var context = CreateContext(
            primaryEmpireName: "Ancient Watchers",
            primaryEmpireNativePopulationMillions: 950,
            primaryEmpirePlanets: 1,
            primaryEmpireIsFallen: true,
            primaryWorld: CreateWorld(
                101,
                "Helios",
                colonyId: 201,
                colonyName: "Helios Prime",
                controllingEmpireId: 3,
                foundingEmpireId: 2),
            additionalEmpires:
            [
                CreateEmpire(
                    3,
                    "Orion Compact",
                    foundingWorldId: 102,
                    primaryRaceId: 1,
                    planets: 3,
                    nativePopulationMillions: 1600)
            ],
            additionalWorlds:
            [
                CreateWorld(
                    102,
                    "Nova Helios",
                    colonyId: 202,
                    colonyName: "Nova Helios Prime",
                    controllingEmpireId: 3,
                    foundingEmpireId: 3)
            ]);

        ConfigureServices(context, queryService: new DelayedEmpireQueryService(context, TimeSpan.FromMilliseconds(400)));

        var cut = Render<Empires>();

        cut.Find("[data-testid='fallen-empire-filter-toggle']").Change(true);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Searching empires with the current filters...", cut.Markup);
            Assert.NotNull(cut.Find("[data-testid='fallen-empire-filter-toggle']").GetAttribute("checked"));
        });

        cut.Find("[data-testid='fallen-empire-filter-toggle']").Change(false);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Searching empires with the current filters...", cut.Markup);
            Assert.Null(cut.Find("[data-testid='fallen-empire-filter-toggle']").GetAttribute("disabled"));
            Assert.Null(cut.Find("[data-testid='fallen-empire-filter-toggle']").GetAttribute("checked"));
        });

        cut.WaitForAssertion(() =>
        {
            var visibleRows = cut.FindAll(".record-row");
            Assert.Equal(2, visibleRows.Count);
            Assert.Contains("Ancient Watchers", cut.Markup);
            Assert.Contains("Orion Compact", cut.Markup);
            Assert.DoesNotContain("Searching empires with the current filters...", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RoundsPopulationDisplayToNearestIllionAndKeepsFullValueInTooltip()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ConfigureServices(CreateContext(
            primaryEmpireName: "Maloyian Church",
            primaryEmpireNativePopulationMillions: 41_954_779));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/sector-explorer/empires?sectorId=7&empireId=2");

        var cut = Render<Empires>();

        cut.WaitForAssertion(() =>
        {
            var nativePopulationValue = cut.FindAll("dt")
                .Single(item => item.TextContent.Trim() == "Native population")
                .ParentElement!
                .QuerySelector("dd abbr");

            Assert.NotNull(nativePopulationValue);
            Assert.Equal("42 trillion", nativePopulationValue!.TextContent.Trim());
            Assert.Equal("41,954,779,000,000", nativePopulationValue.GetAttribute("title"));
            Assert.Contains("Member;", cut.Markup);
            Assert.Contains("42 trillion", cut.Markup);
        });
    }

    private void ConfigureServices(
        StarWinExplorerContext context,
        IReadOnlyDictionary<(EntityNoteTargetKind TargetKind, int TargetId), EntityNote>? notes = null,
        IStarWinExplorerQueryService? queryService = null)
    {
        notes ??= new Dictionary<(EntityNoteTargetKind, int), EntityNote>();
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        Services.AddSingleton<IStarWinExplorerContextService>(new FakeExplorerContextService(context));
        Services.AddSingleton<IStarWinExplorerQueryService>(queryService ?? new FakeExplorerQueryService(context, notes));
        Services.AddSingleton<IStarWinSearchService>(new FakeSearchService());
        Services.AddSingleton<IStarWinImageService>(new FakeImageService());
        Services.AddSingleton<IStarWinEntityNameService>(new FakeEntityNameService());
        Services.AddSingleton<IStarWinEntityNoteService>(new FakeEntityNoteService(notes));
    }

    private static StarWinExplorerContext CreateContext(
        string primaryEmpireName = "Orion Compact",
        int primaryEmpirePlanets = 4,
        long primaryEmpireNativePopulationMillions = 3200,
        bool primaryEmpireIsFallen = false,
        World? primaryWorld = null,
        IEnumerable<AlienRace>? additionalRaces = null,
        IEnumerable<Empire>? additionalEmpires = null,
        IEnumerable<World>? additionalWorlds = null)
    {
        var world = primaryWorld ?? CreateWorld(
            101,
            "Helios",
            colonyId: 201,
            colonyName: "Helios Prime",
            controllingEmpireId: 2,
            foundingEmpireId: 2);

        var system = new StarSystem
        {
            Id = 11,
            SectorId = 7,
            Name = "Helios"
        };
        system.Worlds.Add(world);

        if (additionalWorlds is not null)
        {
            foreach (var additionalWorld in additionalWorlds)
            {
                system.Worlds.Add(additionalWorld);
            }
        }

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

        var races = new List<AlienRace> { race };
        if (additionalRaces is not null)
        {
            races.AddRange(additionalRaces);
        }

        var empire = CreateEmpire(
            2,
            primaryEmpireName,
            foundingWorldId: world.Id,
            primaryRaceId: 1,
            planets: primaryEmpirePlanets,
            nativePopulationMillions: primaryEmpireNativePopulationMillions,
            isFallen: primaryEmpireIsFallen);

        var empires = new List<Empire> { empire };
        if (additionalEmpires is not null)
        {
            empires.AddRange(additionalEmpires);
        }

        return new StarWinExplorerContext([sector], sector, races, empires, []);
    }

    private static World CreateWorld(
        int worldId,
        string worldName,
        int colonyId,
        string colonyName,
        int controllingEmpireId,
        int foundingEmpireId,
        int colonyRaceId = 1,
        string colonyRaceName = "Human",
        long estimatedPopulation = 3_200_000_000,
        IEnumerable<ColonyDemographic>? demographics = null)
    {
        var world = new World
        {
            Id = worldId,
            Name = worldName,
            WorldType = "Terran",
            AtmosphereType = "Breathable",
            StarSystemId = 11
        };

        world.Colony = new Colony
        {
            Id = colonyId,
            WorldId = world.Id,
            Name = colonyName,
            ColonyClass = "Capital",
            EstimatedPopulation = estimatedPopulation,
            RaceId = (ushort)colonyRaceId,
            ColonistRaceName = colonyRaceName,
            ControllingEmpireId = controllingEmpireId,
            FoundingEmpireId = foundingEmpireId,
            AllegianceId = (ushort)controllingEmpireId,
            AllegianceName = controllingEmpireId == 2 ? "Orion Compact" : $"Empire {controllingEmpireId}"
        };

        if (demographics is not null)
        {
            foreach (var demographic in demographics)
            {
                world.Colony.Demographics.Add(demographic);
            }
        }

        return world;
    }

    private static Empire CreateEmpire(
        int empireId,
        string name,
        int foundingWorldId,
        int primaryRaceId,
        int planets,
        long nativePopulationMillions,
        bool isFallen = false)
    {
        var empire = new Empire
        {
            Id = empireId,
            Name = name,
            Planets = planets,
            MilitaryPower = 9,
            NativePopulationMillions = nativePopulationMillions,
            IsFallen = isFallen
        };
        empire.CivilizationProfile.TechLevel = 9;
        empire.Founding.FoundingWorldId = foundingWorldId;
        empire.Founding.FoundingRaceId = primaryRaceId;
        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = primaryRaceId,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = nativePopulationMillions
        });

        return empire;
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

    private class FakeExplorerQueryService(
        StarWinExplorerContext context,
        IReadOnlyDictionary<(EntityNoteTargetKind TargetKind, int TargetId), EntityNote> notes) : IStarWinExplorerQueryService
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

        public virtual Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default)
        {
            var races = GetSectorRaces(sectorId)
                .Select(race => new ExplorerLookupOption(race.Id, race.Name))
                .OrderBy(option => option.Name)
                .ThenBy(option => option.Id)
                .ToList();

            return Task.FromResult(new ExplorerEmpireFilterOptions(races));
        }

        public virtual Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default)
        {
            var items = GetSectorEmpires(request.SectorId)
                .Where(empire => MatchesEmpireFilters(request, empire))
                .OrderBy(empire => empire.Name)
                .ThenBy(empire => empire.Id)
                .Select(empire =>
                {
                    var (controlledWorldCount, trackedWorldCount) = GetEmpireWorldCounts(request.SectorId, empire.Id);
                    return new ExplorerEmpireListItem(
                        empire.Id,
                        empire.Name,
                        controlledWorldCount,
                        trackedWorldCount,
                        empire.CivilizationProfile.TechLevel + 2,
                        empire.IsFallen);
                })
                .ToList();

            var page = items.Skip(request.Offset).Take(request.Limit + 1).ToList();
            var hasMore = page.Count > request.Limit;
            return Task.FromResult(new ExplorerEmpireListPage(page.Take(request.Limit).ToList(), hasMore));
        }

        public virtual Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
        {
            var empire = GetSectorEmpires(sectorId).FirstOrDefault(item => item.Id == empireId);
            if (empire is null)
            {
                return Task.FromResult<ExplorerEmpireListItem?>(null);
            }

            return Task.FromResult<ExplorerEmpireListItem?>(new ExplorerEmpireListItem(
                empire.Id,
                empire.Name,
                GetEmpireWorldCounts(sectorId, empire.Id).ControlledWorldCount,
                GetEmpireWorldCounts(sectorId, empire.Id).TrackedWorldCount,
                empire.CivilizationProfile.TechLevel + 2,
                empire.IsFallen));
        }

        public virtual Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default)
        {
            var sector = context.Sectors.FirstOrDefault(item => item.Id == sectorId);
            var empire = GetSectorEmpires(sectorId).FirstOrDefault(item => item.Id == empireId);
            if (sector is null || empire is null)
            {
                return Task.FromResult<ExplorerEmpireDetail?>(null);
            }

            var homeWorld = sector.Systems
                .SelectMany(system => system.Worlds)
                .FirstOrDefault(world => world.Id == empire.Founding.FoundingWorldId);

            var colonies = sector.Systems
                .SelectMany(system => system.Worlds
                    .Where(world => world.Colony is not null
                        && (world.Colony!.ControllingEmpireId == empireId || world.Colony.FoundingEmpireId == empireId))
                    .Select(world => new ExplorerEmpireColonyListing(
                        world.Colony!.Id,
                        string.IsNullOrWhiteSpace(world.Colony.Name) ? world.Colony.ColonyClass : world.Colony.Name,
                        world.Colony.EstimatedPopulation,
                        world.Colony.ControllingEmpireId == empireId,
                        world.Colony.ControllingEmpireId,
                        world.Colony.ControllingEmpireId is int controllingEmpireId
                            ? context.Empires.FirstOrDefault(item => item.Id == controllingEmpireId)?.Name
                            : null,
                        world.Id,
                        world.Name,
                        system.Id,
                        system.Name)))
                .OrderByDescending(item => item.EstimatedPopulation)
                .ThenBy(item => item.WorldName)
                .ThenBy(item => item.ColonyId)
                .ToList();

            var memberRaces = BuildEmpireRaceDetails(sector, empire, empireId);
            var relationships = empire.Contacts
                .OrderBy(contact => contact.RelationCode)
                .ThenBy(contact => contact.OtherEmpireId)
                .Select(contact => new ExplorerEmpireRelationshipListing(
                    contact.OtherEmpireId,
                    context.Empires.FirstOrDefault(item => item.Id == contact.OtherEmpireId)?.Name ?? $"Empire {contact.OtherEmpireId}",
                    contact.Relation,
                    contact.Age))
                .ToList();
            var modifierDetail = BuildEmpireCivilizationModifierDetail(empire);

            return Task.FromResult<ExplorerEmpireDetail?>(new ExplorerEmpireDetail(
                sectorId,
                empire,
                homeWorld,
                memberRaces,
                colonies,
                relationships,
                colonies.Count(item => item.IsControlled),
                empire.IsFallen,
                modifierDetail));
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

        private IReadOnlyList<AlienRace> GetSectorRaces(int sectorId)
        {
            var sector = context.Sectors.FirstOrDefault(item => item.Id == sectorId);
            if (sector is null)
            {
                return [];
            }

            var raceIds = new HashSet<int>();
            foreach (var empire in GetSectorEmpires(sectorId))
            {
                foreach (var membership in empire.RaceMemberships)
                {
                    raceIds.Add(membership.RaceId);
                }
            }

            foreach (var world in sector.Systems.SelectMany(system => system.Worlds))
            {
                if (world.AlienRaceId is int raceId)
                {
                    raceIds.Add(raceId);
                }

                if (world.Colony?.RaceId is ushort colonyRaceId)
                {
                    raceIds.Add(colonyRaceId);
                }
            }

            return context.AlienRaces.Where(race => raceIds.Contains(race.Id)).ToList();
        }

        private IReadOnlyList<Empire> GetSectorEmpires(int sectorId)
        {
            var sector = context.Sectors.FirstOrDefault(item => item.Id == sectorId);
            if (sector is null)
            {
                return [];
            }

            var empireIds = new HashSet<int>();
            foreach (var world in sector.Systems.SelectMany(system => system.Worlds))
            {
                if (world.Colony?.ControllingEmpireId is int controllingEmpireId)
                {
                    empireIds.Add(controllingEmpireId);
                }

                if (world.Colony?.FoundingEmpireId is int foundingEmpireId)
                {
                    empireIds.Add(foundingEmpireId);
                }
            }

            return context.Empires.Where(empire => empireIds.Contains(empire.Id)).ToList();
        }

        private (int ControlledWorldCount, int TrackedWorldCount) GetEmpireWorldCounts(int sectorId, int empireId)
        {
            var sector = context.Sectors.FirstOrDefault(item => item.Id == sectorId);
            if (sector is null)
            {
                return (0, 0);
            }

            var worlds = sector.Systems.SelectMany(system => system.Worlds)
                .Where(world => world.Colony is not null)
                .ToList();

            var controlledWorldCount = worlds
                .Where(world => world.Colony!.ControllingEmpireId == empireId)
                .Select(world => world.Id)
                .Distinct()
                .Count();

            var trackedWorldCount = worlds
                .Where(world => world.Colony!.ControllingEmpireId == empireId || world.Colony.FoundingEmpireId == empireId)
                .Select(world => world.Id)
                .Distinct()
                .Count();

            return (controlledWorldCount, trackedWorldCount);
        }

        private bool MatchesEmpireFilters(ExplorerEmpireListPageRequest request, Empire empire)
        {
            if (request.RaceId is int raceId && !empire.RaceMemberships.Any(membership => membership.RaceId == raceId))
            {
                return false;
            }

            if (request.FallenOnly && !empire.IsFallen)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return true;
            }

            var query = request.Query.Trim();
            return ContainsQuery(empire.Name, query)
                || ContainsQuery(GetNoteMarkdown(EntityNoteTargetKind.EmpireSummary, empire.Id), query)
                || ContainsQuery(GetNoteMarkdown(EntityNoteTargetKind.Empire, empire.Id), query);
        }

        private static bool ContainsQuery(string? value, string query)
        {
            return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
        }

        private string? GetNoteMarkdown(EntityNoteTargetKind targetKind, int targetId)
        {
            return notes.TryGetValue((targetKind, targetId), out var note)
                ? note.Markdown
                : null;
        }

        private IReadOnlyList<ExplorerEmpireRaceMembershipDetail> BuildEmpireRaceDetails(StarWinSector sector, Empire empire, int empireId)
        {
            var membershipByRaceId = empire.RaceMemberships.ToDictionary(membership => membership.RaceId);
            var controlledPopulationRows = sector.Systems
                .SelectMany(system => system.Worlds)
                .Where(world => world.Colony?.ControllingEmpireId == empireId)
                .SelectMany(world =>
                {
                    var colony = world.Colony!;
                    if (colony.Demographics.Count > 0)
                    {
                        return colony.Demographics.Select(demographic => new TestEmpireRacePopulationRow(
                            demographic.RaceId,
                            ResolveRaceName(demographic.RaceId, demographic.RaceName),
                            demographic.Population,
                            colony.FoundingEmpireId == empireId,
                            colony.FoundingEmpireId != empireId));
                    }

                    return
                    [
                        new TestEmpireRacePopulationRow(
                            colony.RaceId,
                            ResolveRaceName(colony.RaceId, colony.ColonistRaceName),
                            colony.EstimatedPopulation,
                            colony.FoundingEmpireId == empireId,
                            colony.FoundingEmpireId != empireId)
                    ];
                })
                .ToList();

            if (controlledPopulationRows.Count > 0)
            {
                var totalPopulation = controlledPopulationRows.Sum(row => row.Population);
                return controlledPopulationRows
                    .GroupBy(row => row.RaceId)
                    .Select(group =>
                    {
                        membershipByRaceId.TryGetValue(group.Key, out var membership);
                        var population = group.Sum(item => item.Population);
                        return new ExplorerEmpireRaceMembershipDetail(
                            group.Key,
                            group.Select(item => item.RaceName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"Race {group.Key}",
                            membership?.Role ?? (group.Any(item => item.ForeignFounded) ? EmpireRaceRole.Subjugated : EmpireRaceRole.Member),
                            ToPopulationMillions(population),
                            CalculatePopulationPercent(population, totalPopulation),
                            membership?.IsPrimary ?? empire.Founding.FoundingRaceId == group.Key);
                    })
                    .OrderByDescending(item => item.PopulationMillions)
                    .ThenByDescending(item => item.IsPrimary)
                    .ThenBy(item => item.RaceName)
                    .ThenBy(item => item.RaceId)
                    .ToList();
            }

            var totalMembershipPopulation = empire.RaceMemberships.Sum(membership => membership.PopulationMillions);
            return empire.RaceMemberships
                .Select(membership => new ExplorerEmpireRaceMembershipDetail(
                    membership.RaceId,
                    ResolveRaceName(membership.RaceId),
                    membership.Role,
                    membership.PopulationMillions,
                    CalculatePopulationPercent(membership.PopulationMillions, totalMembershipPopulation),
                    membership.IsPrimary))
                .OrderByDescending(item => item.PopulationMillions)
                .ThenByDescending(item => item.IsPrimary)
                .ThenBy(item => item.RaceName)
                .ThenBy(item => item.RaceId)
                .ToList();
        }

        private string ResolveRaceName(int raceId, string? fallbackName = null)
        {
            var race = context.AlienRaces.FirstOrDefault(item => item.Id == raceId);
            return string.IsNullOrWhiteSpace(race?.Name)
                ? (string.IsNullOrWhiteSpace(fallbackName) ? $"Race {raceId}" : fallbackName)
                : race.Name;
        }

        private ExplorerEmpireCivilizationModifierDetail BuildEmpireCivilizationModifierDetail(Empire empire)
        {
            return new ExplorerEmpireCivilizationModifierDetail(
                [
                    BuildCivilizationTraitModifier("Militancy", empire.CivilizationModifiers.Militancy),
                    BuildCivilizationTraitModifier("Determination", empire.CivilizationModifiers.Determination),
                    BuildCivilizationTraitModifier("Racial tolerance", empire.CivilizationModifiers.RacialTolerance),
                    BuildCivilizationTraitModifier("Progressiveness", empire.CivilizationModifiers.Progressiveness),
                    BuildCivilizationTraitModifier("Loyalty", empire.CivilizationModifiers.Loyalty),
                    BuildCivilizationTraitModifier("Social cohesion", empire.CivilizationModifiers.SocialCohesion),
                    BuildCivilizationTraitModifier("Art", empire.CivilizationModifiers.Art),
                    BuildCivilizationTraitModifier("Individualism", empire.CivilizationModifiers.Individualism)
                ]);
        }

        private static ExplorerCivilizationTraitModifier BuildCivilizationTraitModifier(
            string name,
            int modifier)
        {
            return new ExplorerCivilizationTraitModifier(name, modifier);
        }

        private static long ToPopulationMillions(long population)
        {
            if (population <= 0)
            {
                return 0;
            }

            return Math.Max(1L, (long)Math.Round(population / 1_000_000d, MidpointRounding.AwayFromZero));
        }

        private static decimal CalculatePopulationPercent(long population, long totalPopulation)
        {
            if (population <= 0 || totalPopulation <= 0)
            {
                return 0m;
            }

            return Math.Round((decimal)population * 100m / totalPopulation, 1, MidpointRounding.AwayFromZero);
        }

    }

    private sealed class DelayedEmpireQueryService(StarWinExplorerContext context, TimeSpan delay)
        : FakeExplorerQueryService(context, new Dictionary<(EntityNoteTargetKind, int), EntityNote>())
    {
        public override async Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(request.Query) || request.RaceId.HasValue || request.FallenOnly)
            {
                await Task.Delay(delay, cancellationToken);
            }

            return await base.LoadEmpireListPageAsync(request, cancellationToken);
        }
    }

    private sealed record TestEmpireRacePopulationRow(
        int RaceId,
        string RaceName,
        long Population,
        bool FoundedByEmpire,
        bool ForeignFounded);

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

    private sealed class FakeEntityNoteService(
        IReadOnlyDictionary<(EntityNoteTargetKind TargetKind, int TargetId), EntityNote> notes) : IStarWinEntityNoteService
    {
        public Task<EntityNote?> GetNoteAsync(EntityNoteTargetKind targetKind, int targetId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                notes.TryGetValue((targetKind, targetId), out var note)
                    ? note
                    : null);
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
