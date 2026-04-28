using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;
using StarWin.Infrastructure.Services;
using Xunit;

namespace StarWin.Infrastructure.Tests.Explorer;

public sealed class StarWinExplorerQueryServiceTests
{
    [Fact]
    public async Task LoadAlienRaceFilterOptionsAsync_returns_distinct_database_backed_filter_values()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var options = await service.LoadAlienRaceFilterOptionsAsync(1);

            Assert.Contains("Temperate", options.EnvironmentTypes);
            Assert.Contains("Arid", options.EnvironmentTypes);
            Assert.Contains("Humanoid", options.AppearanceTypes);
            Assert.Contains("Reptilian", options.AppearanceTypes);
            Assert.Contains(6, options.StarWinTechLevels);
            Assert.Contains(8, options.StarWinTechLevels);
            Assert.Contains("8^", options.GurpsTechLevels);
            Assert.Contains("10", options.GurpsTechLevels);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadAlienRaceListPageAsync_applies_text_type_cost_and_tech_filters_in_database()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var noteSearch = await service.LoadAlienRaceListPageAsync(new ExplorerAlienRaceListPageRequest(1, 0, 30, Query: "diplomats"));
            Assert.Single(noteSearch.Items);
            Assert.Equal("Aurelian", noteSearch.Items[0].Name);

            var environmentSearch = await service.LoadAlienRaceListPageAsync(new ExplorerAlienRaceListPageRequest(1, 0, 30, EnvironmentType: "Arid"));
            Assert.Single(environmentSearch.Items);
            Assert.Equal("Krell", environmentSearch.Items[0].Name);

            var techSearch = await service.LoadAlienRaceListPageAsync(new ExplorerAlienRaceListPageRequest(1, 0, 30, GurpsTechLevel: "8^"));
            Assert.Single(techSearch.Items);
            Assert.Equal("Aurelian", techSearch.Items[0].Name);

            var starWinSearch = await service.LoadAlienRaceListPageAsync(new ExplorerAlienRaceListPageRequest(1, 0, 30, StarWinTechLevel: 8));
            Assert.Single(starWinSearch.Items);
            Assert.Equal("Krell", starWinSearch.Items[0].Name);

            var costSearch = await service.LoadAlienRaceListPageAsync(new ExplorerAlienRaceListPageRequest(1, 0, 30, MaxTotalPointCost: 5));
            Assert.Single(costSearch.Items);
            Assert.Equal("Krell", costSearch.Items[0].Name);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadEmpireFilterOptionsAsync_returns_distinct_race_options_for_sector_empires()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var options = await service.LoadEmpireFilterOptionsAsync(1);

            Assert.Collection(
                options.Races.OrderBy(item => item.Id),
                item =>
                {
                    Assert.Equal(1, item.Id);
                    Assert.Equal("Aurelian", item.Name);
                },
                item =>
                {
                    Assert.Equal(2, item.Id);
                    Assert.Equal("Krell", item.Name);
                });
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadEmpireListPageAsync_applies_query_and_race_filters_and_marks_fallen_empires()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var querySearch = await service.LoadEmpireListPageAsync(new ExplorerEmpireListPageRequest(1, 0, 30, Query: "Concord"));
            Assert.Single(querySearch.Items);
            Assert.Equal("Aurelian Concord", querySearch.Items[0].Name);
            Assert.Equal(1, querySearch.Items[0].ControlledWorldCount);
            Assert.Equal(1, querySearch.Items[0].TrackedWorldCount);

            var summarySearch = await service.LoadEmpireListPageAsync(new ExplorerEmpireListPageRequest(1, 0, 30, Query: "trade coalition"));
            Assert.Single(summarySearch.Items);
            Assert.Equal("Aurelian Concord", summarySearch.Items[0].Name);

            var notesSearch = await service.LoadEmpireListPageAsync(new ExplorerEmpireListPageRequest(1, 0, 30, Query: "archivists"));
            Assert.Single(notesSearch.Items);
            Assert.Equal("Watcher Remnant", notesSearch.Items[0].Name);

            var raceSearch = await service.LoadEmpireListPageAsync(new ExplorerEmpireListPageRequest(1, 0, 30, RaceId: 2));
            Assert.Single(raceSearch.Items);
            Assert.Equal("Krell Reach", raceSearch.Items[0].Name);

            var fallenSearch = await service.LoadEmpireListPageAsync(new ExplorerEmpireListPageRequest(1, 0, 30, Query: "Watcher"));
            Assert.Single(fallenSearch.Items);
            Assert.True(fallenSearch.Items[0].IsFallen);

            var fallenOnlySearch = await service.LoadEmpireListPageAsync(new ExplorerEmpireListPageRequest(1, 0, 30, FallenOnly: true));
            Assert.Single(fallenOnlySearch.Items);
            Assert.Equal("Watcher Remnant", fallenOnlySearch.Items[0].Name);
            Assert.Equal(0, fallenOnlySearch.Items[0].ControlledWorldCount);
            Assert.Equal(1, fallenOnlySearch.Items[0].TrackedWorldCount);
            Assert.True(fallenOnlySearch.Items[0].IsFallen);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadEmpireDetailAsync_returns_targeted_homeworld_memberships_and_colonies()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var detail = await service.LoadEmpireDetailAsync(1, 201);

            Assert.NotNull(detail);
            Assert.Equal("Aurelian Concord", detail!.Empire.Name);
            Assert.Equal("Aurel Prime", detail.HomeWorld?.Name);
            Assert.Single(detail.MemberRaces);
            Assert.Equal("Aurelian", detail.MemberRaces[0].RaceName);
            Assert.Single(detail.Colonies);
            Assert.Equal("Far Aurel", detail.Colonies[0].WorldName);
            Assert.True(detail.Colonies[0].IsControlled);
            Assert.Equal("Aurelian Concord", detail.Colonies[0].ControllingEmpireName);
            Assert.Equal(1, detail.ControlledColonyCount);
            Assert.False(detail.IsFallen);
            Assert.Equal(2, detail.Relationships.Count);
            Assert.Equal("Krell Reach", detail.Relationships[0].OtherEmpireName);
            Assert.Equal("Trade", detail.Relationships[0].Relation);
            Assert.NotNull(detail.CivilizationModifierDetail);
            Assert.Contains(
                detail.CivilizationModifierDetail.Traits,
                trait => trait.Name == "Militancy"
                    && trait.Modifier == 2);
            Assert.Contains(
                detail.CivilizationModifierDetail.Traits,
                trait => trait.Name == "Loyalty"
                    && trait.Modifier == 0);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadEmpireDetailAsync_includes_conquered_world_races_in_empire_demographics()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var detail = await service.LoadEmpireDetailAsync(1, 202);

            Assert.NotNull(detail);
            Assert.Equal(2, detail!.MemberRaces.Count);
            Assert.Collection(
                detail.MemberRaces,
                item =>
                {
                    Assert.Equal("Krell", item.RaceName);
                    Assert.Equal(EmpireRaceRole.Member, item.Role);
                    Assert.Equal(450, item.PopulationMillions);
                    Assert.Equal(94.7m, item.PopulationPercent);
                    Assert.True(item.IsPrimary);
                },
                item =>
                {
                    Assert.Equal("Aurelian", item.RaceName);
                    Assert.Equal(EmpireRaceRole.Subjugated, item.Role);
                    Assert.Equal(25, item.PopulationMillions);
                    Assert.Equal(5.3m, item.PopulationPercent);
                    Assert.False(item.IsPrimary);
                });
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadEmpireDetailAsync_marks_tracked_conquered_colonies_with_current_controller()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var detail = await service.LoadEmpireDetailAsync(1, 203);

            Assert.NotNull(detail);
            Assert.Single(detail!.Colonies);
            Assert.False(detail.Colonies[0].IsControlled);
            Assert.Equal("Krell Reach", detail.Colonies[0].ControllingEmpireName);
            Assert.Equal(0, detail.ControlledColonyCount);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadEmpireDetailAsync_orders_member_races_by_population_descending()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var conqueredColony = await seedContext.Colonies.SingleAsync(colony => colony.Id == 1003);
            conqueredColony.EstimatedPopulation = 600_000_000;
            await seedContext.SaveChangesAsync();

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var detail = await service.LoadEmpireDetailAsync(1, 202);

            Assert.NotNull(detail);
            Assert.Equal(2, detail!.MemberRaces.Count);
            Assert.Equal("Aurelian", detail.MemberRaces[0].RaceName);
            Assert.Equal(600, detail.MemberRaces[0].PopulationMillions);
            Assert.False(detail.MemberRaces[0].IsPrimary);
            Assert.Equal("Krell", detail.MemberRaces[1].RaceName);
            Assert.Equal(450, detail.MemberRaces[1].PopulationMillions);
            Assert.True(detail.MemberRaces[1].IsPrimary);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task EmpireQueries_resolve_placeholder_race_and_empire_names_from_founding_data()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var placeholderHomeWorld = new World
            {
                Id = 105,
                Name = "Hersey 1",
                StarSystemId = 20,
                AlienRaceId = 10,
                WorldType = "Terran"
            };
            placeholderHomeWorld.Colony = new Colony
            {
                Id = 1004,
                WorldId = 105,
                RaceId = 10,
                ColonyClass = "Capital",
                EstimatedPopulation = 200_000_000,
                ControllingEmpireId = 210,
                FoundingEmpireId = 210,
                AllegianceId = 210
            };

            var placeholderRace = new AlienRace
            {
                Id = 10,
                Name = "Race 10",
                HomePlanetId = 105
            };

            var placeholderEmpire = new Empire
            {
                Id = 210,
                Name = "Empire 10",
                GovernmentType = "Monarchy",
                NativePopulationMillions = 200
            };
            placeholderEmpire.Founding.FoundingWorldId = 105;
            placeholderEmpire.Founding.FoundingRaceId = 10;
            placeholderEmpire.Founding.Origin = EmpireOrigin.NativeHomeworld;
            placeholderEmpire.RaceMemberships.Add(new EmpireRaceMembership
            {
                RaceId = 10,
                IsPrimary = true,
                Role = EmpireRaceRole.Founder,
                PopulationMillions = 200
            });

            seedContext.Worlds.Add(placeholderHomeWorld);
            seedContext.AlienRaces.Add(placeholderRace);
            seedContext.Empires.Add(placeholderEmpire);
            await seedContext.SaveChangesAsync();

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var listItem = await service.LoadEmpireListItemAsync(1, 210);
            var detail = await service.LoadEmpireDetailAsync(1, 210);

            Assert.NotNull(listItem);
            Assert.Equal("Kingdom of Herseyian", listItem!.Name);
            Assert.NotNull(detail);
            Assert.Equal("Kingdom of Herseyian", detail!.Empire.Name);
            Assert.Single(detail.MemberRaces);
            Assert.Equal("Herseyian", detail.MemberRaces[0].RaceName);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadReligionFilterOptionsAsync_returns_distinct_types_for_sector_religions()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var options = await service.LoadReligionFilterOptionsAsync(1);

            Assert.Equal(["Animism", "Church"], options.Types);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task LoadReligionListPageAsync_and_detail_return_sector_scoped_religion_data()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var seedContext = CreateDbContext(databasePath);
            await seedContext.Database.EnsureCreatedAsync();
            await SeedExplorerDataAsync(seedContext);

            var service = new StarWinExplorerQueryService(CreateFactory(databasePath));

            var filteredPage = await service.LoadReligionListPageAsync(new ExplorerReligionListPageRequest(1, 0, 30, Query: "Solar"));
            Assert.Single(filteredPage.Items);
            Assert.Equal("Solar Doctrine", filteredPage.Items[0].Name);
            Assert.Equal("Church", filteredPage.Items[0].Type);
            Assert.Equal(2, filteredPage.Items[0].EmpireCount);

            var detail = await service.LoadReligionDetailAsync(1, 301);

            Assert.NotNull(detail);
            Assert.Equal("Solar Doctrine", detail!.Religion.Name);
            Assert.Collection(
                detail.Empires,
                item =>
                {
                    Assert.Equal("Aurelian Concord", item.EmpireName);
                    Assert.Equal(100m, item.PopulationPercent);
                    Assert.Equal(1200, item.TotalPopulationMillions);
                    Assert.Equal((byte)6, item.StarWinTechLevel);
                },
                item =>
                {
                    Assert.Equal("Watcher Remnant", item.EmpireName);
                    Assert.Equal(40m, item.PopulationPercent);
                    Assert.Equal(30, item.TotalPopulationMillions);
                    Assert.Equal((byte)7, item.StarWinTechLevel);
                });

            Assert.Single(detail.Races);
            Assert.Equal("Aurelian", detail.Races[0].RaceName);
            Assert.Equal(2, detail.Races[0].EmpireCount);
            Assert.Equal(1230, detail.Races[0].TotalPopulationMillions);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    private static async Task SeedExplorerDataAsync(StarWinDbContext dbContext)
    {
        var sector = new StarWinSector { Id = 1, Name = "Delcora" };
        var homeSystem = new StarSystem { Id = 10, SectorId = 1, Name = "Helios", AllegianceId = 201 };
        var remoteSystem = new StarSystem { Id = 20, SectorId = 1, Name = "Nadir", AllegianceId = 201 };

        var aurelianHome = new World
        {
            Id = 101,
            Name = "Aurel Prime",
            StarSystemId = 10,
            AlienRaceId = 1,
            WorldType = "Terran"
        };
        var aurelianColonyWorld = new World
        {
            Id = 102,
            Name = "Far Aurel",
            StarSystemId = 20,
            WorldType = "Terran"
        };
        aurelianColonyWorld.Colony = new Colony
        {
            Id = 1001,
            WorldId = 102,
            RaceId = 1,
            ColonyClass = "Outpost",
            EstimatedPopulation = 120_000_000,
            ControllingEmpireId = 201,
            FoundingEmpireId = 201,
            AllegianceId = 201
        };

        var krellHome = new World
        {
            Id = 103,
            Name = "Krellos",
            StarSystemId = 10,
            AlienRaceId = 2,
            WorldType = "Desert"
        };
        krellHome.Colony = new Colony
        {
            Id = 1002,
            WorldId = 103,
            RaceId = 2,
            ColonyClass = "Capital",
            EstimatedPopulation = 450_000_000,
            ControllingEmpireId = 202,
            FoundingEmpireId = 202,
            AllegianceId = 202
        };

        var fallenWorld = new World
        {
            Id = 104,
            Name = "Watcher Rest",
            StarSystemId = 20,
            WorldType = "Terran"
        };
        fallenWorld.Colony = new Colony
        {
            Id = 1003,
            WorldId = 104,
            RaceId = 1,
            ColonyClass = "Archive",
            EstimatedPopulation = 25_000_000,
            ControllingEmpireId = 202,
            FoundingEmpireId = 203,
            AllegianceId = 202
        };

        homeSystem.Worlds.Add(aurelianHome);
        homeSystem.Worlds.Add(krellHome);
        remoteSystem.Worlds.Add(aurelianColonyWorld);
        remoteSystem.Worlds.Add(fallenWorld);
        sector.Systems.Add(homeSystem);
        sector.Systems.Add(remoteSystem);

        var aurelian = new AlienRace
        {
            Id = 1,
            Name = "Aurelian",
            HomePlanetId = 101,
            EnvironmentType = "Temperate",
            AppearanceType = "Humanoid",
            BodyChemistry = "Carbon",
            BodyCoverType = "Hard shell",
            Diet = "Omnivore",
            Reproduction = "Sexual",
            ReproductionMethod = "Live birth",
            MassKg = 140,
            SizeCm = 190
        };
        aurelian.BiologyProfile.Body = 14;
        aurelian.BiologyProfile.Mind = 12;
        aurelian.BiologyProfile.Speed = 12;
        aurelian.BiologyProfile.Lifespan = 40;
        aurelian.BiologyProfile.PsiPower = 2;
        aurelian.BiologyProfile.PsiRating = PsiPowerRating.Good;
        aurelian.CivilizationProfile.Militancy = 11;
        aurelian.CivilizationProfile.Determination = 12;
        aurelian.CivilizationProfile.RacialTolerance = 9;
        aurelian.CivilizationProfile.Progressiveness = 8;
        aurelian.CivilizationProfile.Loyalty = 17;
        aurelian.CivilizationProfile.SocialCohesion = 16;
        aurelian.CivilizationProfile.Art = 10;
        aurelian.CivilizationProfile.Individualism = 7;

        var krell = new AlienRace
        {
            Id = 2,
            Name = "Krell",
            HomePlanetId = 103,
            EnvironmentType = "Arid",
            AppearanceType = "Reptilian",
            BodyChemistry = "Carbon",
            BodyCoverType = "Soft skin",
            Diet = "Omnivore",
            Reproduction = "Sexual",
            ReproductionMethod = "Eggs",
            MassKg = 80,
            SizeCm = 175
        };
        krell.BiologyProfile.Body = 10;
        krell.BiologyProfile.Mind = 10;
        krell.BiologyProfile.Speed = 10;
        krell.BiologyProfile.Lifespan = 20;
        krell.BiologyProfile.PsiPower = 0;
        krell.BiologyProfile.PsiRating = PsiPowerRating.None;
        krell.CivilizationProfile.Militancy = 14;
        krell.CivilizationProfile.Determination = 11;
        krell.CivilizationProfile.RacialTolerance = 6;
        krell.CivilizationProfile.Progressiveness = 5;
        krell.CivilizationProfile.Loyalty = 12;
        krell.CivilizationProfile.SocialCohesion = 10;
        krell.CivilizationProfile.Art = 4;
        krell.CivilizationProfile.Individualism = 8;

        var aurelianEmpire = new Empire
        {
            Id = 201,
            Name = "Aurelian Concord",
            GovernmentType = "Council",
            NativePopulationMillions = 1200
        };
        aurelianEmpire.CivilizationProfile.TechLevel = 6;
        aurelianEmpire.CivilizationProfile.Militancy = 13;
        aurelianEmpire.CivilizationProfile.Determination = 11;
        aurelianEmpire.CivilizationProfile.RacialTolerance = 9;
        aurelianEmpire.CivilizationProfile.Progressiveness = 8;
        aurelianEmpire.CivilizationProfile.Loyalty = 15;
        aurelianEmpire.CivilizationProfile.SocialCohesion = 16;
        aurelianEmpire.CivilizationProfile.Art = 10;
        aurelianEmpire.CivilizationProfile.Individualism = 7;
        aurelianEmpire.CivilizationModifiers.Militancy = 2;
        aurelianEmpire.CivilizationModifiers.Determination = -1;
        aurelianEmpire.Founding.FoundingWorldId = 101;
        aurelianEmpire.Founding.FoundingRaceId = 1;
        aurelianEmpire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = 1,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 1200
        });
        aurelianEmpire.Contacts.Add(new EmpireContact
        {
            EmpireId = 201,
            OtherEmpireId = 202,
            Relation = "Trade",
            RelationCode = 2,
            Age = 4
        });
        aurelianEmpire.Contacts.Add(new EmpireContact
        {
            EmpireId = 201,
            OtherEmpireId = 203,
            Relation = "Alliance",
            RelationCode = 3,
            Age = 2
        });

        var krellEmpire = new Empire
        {
            Id = 202,
            Name = "Krell Reach",
            GovernmentType = "Council",
            NativePopulationMillions = 450
        };
        krellEmpire.CivilizationProfile.TechLevel = 8;
        krellEmpire.CivilizationProfile.Militancy = 14;
        krellEmpire.CivilizationProfile.Determination = 11;
        krellEmpire.CivilizationProfile.RacialTolerance = 6;
        krellEmpire.CivilizationProfile.Progressiveness = 5;
        krellEmpire.CivilizationProfile.Loyalty = 12;
        krellEmpire.CivilizationProfile.SocialCohesion = 10;
        krellEmpire.CivilizationProfile.Art = 4;
        krellEmpire.CivilizationProfile.Individualism = 8;
        krellEmpire.Founding.FoundingRaceId = 2;
        krellEmpire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = 2,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 450
        });

        var fallenEmpire = new Empire
        {
            Id = 203,
            Name = "Watcher Remnant",
            GovernmentType = "Council",
            Planets = 1,
            NativePopulationMillions = 30,
            IsFallen = true
        };
        fallenEmpire.CivilizationProfile.TechLevel = 7;
        fallenEmpire.CivilizationProfile.Militancy = 9;
        fallenEmpire.CivilizationProfile.Determination = 10;
        fallenEmpire.CivilizationProfile.RacialTolerance = 9;
        fallenEmpire.CivilizationProfile.Progressiveness = 8;
        fallenEmpire.CivilizationProfile.Loyalty = 17;
        fallenEmpire.CivilizationProfile.SocialCohesion = 16;
        fallenEmpire.CivilizationProfile.Art = 10;
        fallenEmpire.CivilizationProfile.Individualism = 7;
        fallenEmpire.Founding.FoundingWorldId = 104;
        fallenEmpire.Founding.FoundingRaceId = 1;
        fallenEmpire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = 1,
            IsPrimary = false,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 30
        });

        var solarDoctrine = new Religion
        {
            Id = 301,
            Name = "Solar Doctrine",
            Type = "Church"
        };

        var dunePath = new Religion
        {
            Id = 302,
            Name = "Dune Path",
            Type = "Animism"
        };

        dbContext.Sectors.Add(sector);
        dbContext.AlienRaces.AddRange(aurelian, krell);
        dbContext.Empires.AddRange(aurelianEmpire, krellEmpire, fallenEmpire);
        dbContext.Religions.AddRange(solarDoctrine, dunePath);
        dbContext.Set<EmpireReligion>().AddRange(
            new EmpireReligion
            {
                EmpireId = 201,
                ReligionId = 301,
                ReligionName = solarDoctrine.Name,
                PopulationPercent = 100m
            },
            new EmpireReligion
            {
                EmpireId = 203,
                ReligionId = 301,
                ReligionName = solarDoctrine.Name,
                PopulationPercent = 40m
            },
            new EmpireReligion
            {
                EmpireId = 202,
                ReligionId = 302,
                ReligionName = dunePath.Name,
                PopulationPercent = 75m
            });
        dbContext.EntityNotes.Add(new EntityNote
        {
            TargetKind = EntityNoteTargetKind.AlienRace,
            TargetId = 1,
            Markdown = "Ancient diplomats with a strong expansion record."
        });
        dbContext.EntityNotes.AddRange(
            new EntityNote
            {
                TargetKind = EntityNoteTargetKind.EmpireSummary,
                TargetId = 201,
                Markdown = "A frontier trade coalition with a diplomatic charter."
            },
            new EntityNote
            {
                TargetKind = EntityNoteTargetKind.Empire,
                TargetId = 203,
                Markdown = "Reclusive archivists preserving pre-collapse records."
            });

        await dbContext.SaveChangesAsync();
    }

    private static IDbContextFactory<StarWinDbContext> CreateFactory(string databasePath)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new OptionsDbContextFactory(options);
    }

    private static StarWinDbContext CreateDbContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new StarWinDbContext(options);
    }

    private static string CreateTempFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"starwin-explorer-{Guid.NewGuid():N}{extension}");
    }

    private static void DeleteIfExists(string path)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class OptionsDbContextFactory(DbContextOptions<StarWinDbContext> options) : IDbContextFactory<StarWinDbContext>
    {
        public StarWinDbContext CreateDbContext()
        {
            return new StarWinDbContext(options);
        }

        public Task<StarWinDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
