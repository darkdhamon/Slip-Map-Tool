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

        homeSystem.Worlds.Add(aurelianHome);
        homeSystem.Worlds.Add(krellHome);
        remoteSystem.Worlds.Add(aurelianColonyWorld);
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

        var aurelianEmpire = new Empire
        {
            Id = 201,
            Name = "Aurelian Concord",
            GovernmentType = "Council"
        };
        aurelianEmpire.CivilizationProfile.TechLevel = 6;
        aurelianEmpire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = 1,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 1200
        });

        var krellEmpire = new Empire
        {
            Id = 202,
            Name = "Krell Reach",
            GovernmentType = "Council"
        };
        krellEmpire.CivilizationProfile.TechLevel = 8;
        krellEmpire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = 2,
            IsPrimary = true,
            Role = EmpireRaceRole.Member,
            PopulationMillions = 450
        });

        dbContext.Sectors.Add(sector);
        dbContext.AlienRaces.AddRange(aurelian, krell);
        dbContext.Empires.AddRange(aurelianEmpire, krellEmpire);
        dbContext.EntityNotes.Add(new EntityNote
        {
            TargetKind = EntityNoteTargetKind.AlienRace,
            TargetId = 1,
            Markdown = "Ancient diplomats with a strong expansion record."
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
