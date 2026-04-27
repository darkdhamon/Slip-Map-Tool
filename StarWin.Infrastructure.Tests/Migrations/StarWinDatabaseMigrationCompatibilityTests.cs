using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Infrastructure.Data;
using Xunit;

namespace StarWin.Infrastructure.Tests.Migrations;

public sealed class StarWinDatabaseMigrationCompatibilityTests
{
    private const string PreviousMigrationId = "20260425142621_AddHistoryImportDataJson";
    private const string CurrentMigrationId = "20260427064246_AddAlienEmpireImportParity";

    [Fact]
    public async Task Versioned_sqlite_database_migrates_to_issue28_schema_without_losing_legacy_alien_empire_data()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using (var dbContext = CreateSqliteDbContext(databasePath))
            {
                var migrator = dbContext.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigrationId);
                await SeedPreImportParityDataAsync(dbContext);
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["StarWin:DatabaseProvider"] = "Sqlite",
                    ["ConnectionStrings:StarWin"] = $"Data Source={databasePath}"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddStarWinInfrastructure(configuration);

            using var provider = services.BuildServiceProvider();
            await provider.MigrateStarWinDatabaseAsync();

            await AssertPostParityStateAsync(databasePath, expectLegacyColumnsDropped: true);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task Legacy_sqlite_database_without_migration_history_is_upgraded_in_place_to_issue28_schema()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using (var dbContext = CreateSqliteDbContext(databasePath))
            {
                var migrator = dbContext.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigrationId);
                await SeedPreImportParityDataAsync(dbContext);
                await dbContext.Database.ExecuteSqlRawAsync("""DROP TABLE "__EFMigrationsHistory";""");
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["StarWin:DatabaseProvider"] = "Sqlite",
                    ["ConnectionStrings:StarWin"] = $"Data Source={databasePath}"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddStarWinInfrastructure(configuration);

            using var provider = services.BuildServiceProvider();
            await provider.MigrateStarWinDatabaseAsync();

            await AssertPostParityStateAsync(databasePath, expectLegacyColumnsDropped: false);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();
            Assert.True(await TableExistsAsync(connection, "__EFMigrationsHistory"));
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public void SqlServer_migration_script_uses_sqlserver_unbounded_text_types()
    {
        using var dbContext = CreateSqlServerDbContext("Server=(localdb)\\mssqllocaldb;Database=StarWinMigrationScript;Trusted_Connection=True;MultipleActiveResultSets=true");
        var migrator = dbContext.GetService<IMigrator>();

        var script = migrator.GenerateScript(PreviousMigrationId, CurrentMigrationId);

        Assert.Contains("nvarchar(max)", script);
        Assert.Contains("INSERT INTO Religions", script);
        Assert.Contains("UPDATE empire", script);
    }

    private static async Task AssertPostParityStateAsync(string databasePath, bool expectLegacyColumnsDropped)
    {
        await using var dbContext = CreateSqliteDbContext(databasePath);

        var race = await dbContext.AlienRaces.SingleAsync(item => item.Id == 1);
        var empire = await dbContext.Empires.SingleAsync(item => item.Id == 1);
        var religion = await dbContext.Religions.SingleAsync();
        var empireReligion = await dbContext.Set<EmpireReligion>().SingleAsync(item => item.EmpireId == 1);

        Assert.Equal("Legacy Race", race.Name);
        Assert.Equal(11, race.CivilizationProfile.Militancy);
        Assert.Equal(12, race.CivilizationProfile.Determination);
        Assert.Equal(13, race.CivilizationProfile.RacialTolerance);
        Assert.Equal(14, race.CivilizationProfile.Progressiveness);
        Assert.Equal(15, race.CivilizationProfile.Loyalty);
        Assert.Equal(16, race.CivilizationProfile.SocialCohesion);
        Assert.Equal(17, race.CivilizationProfile.Art);
        Assert.Equal(18, race.CivilizationProfile.Individualism);
        Assert.Equal(string.Empty, race.HairType);

        Assert.Equal("Representative Democracy", empire.GovernmentType);
        Assert.Equal("Legacy Faith", religion.Name);
        Assert.Equal("Legacy Faith", empireReligion.ReligionName);
        Assert.Equal(100m, empireReligion.PopulationPercent);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        Assert.True(await ColumnExistsAsync(connection, "AlienRaces", "HairType"));
        Assert.True(await ColumnExistsAsync(connection, "AlienRaces", "CivilizationProfile_Militancy"));
        Assert.True(await ColumnExistsAsync(connection, "Empires", "GovernmentType"));
        if (expectLegacyColumnsDropped)
        {
            Assert.False(await ColumnExistsAsync(connection, "AlienRaces", "Religion"));
            Assert.False(await ColumnExistsAsync(connection, "AlienRaces", "GovernmentType"));
        }
    }

    private static async Task SeedPreImportParityDataAsync(StarWinDbContext dbContext)
    {
        var legacyAttributes = new byte[] { 11, 12, 13, 14, 15, 16, 4, 6, 10, 11, 12, 20, 17, 18, 3 };

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Sectors (Id, Name)
            VALUES (1, 'Legacy Sector');
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO StarSystems (Id, SectorId, Name, Coordinates, AllegianceId, MapCode, LegacySystemId)
            VALUES (10, 1, 'Legacy System', '0101', 0, 0, 10);
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO AlienRaces (
                Id, HomePlanetId, Name, EnvironmentType, BodyChemistry, BodyCoverType, AppearanceType,
                Diet, Reproduction, ReproductionMethod, GovernmentType, Religion, Devotion, DevotionLevel,
                MassKg, SizeCm, LimbPairCount, LegacyAttributes,
                BiologyProfile_Body, BiologyProfile_Mind, BiologyProfile_Speed, BiologyProfile_Lifespan,
                BiologyProfile_PsiPower, BiologyProfile_PsiRating)
            VALUES (
                1, 101, 'Legacy Race', 'Temperate', 'Carbon', 'Skin', 'Humanoid',
                'Omnivore', 'Sexual', 'Live birth', 'Representative Democracy', 'Legacy Faith', 4, 'Moderate',
                72, 175, 2, {0},
                10, 11, 12, 20,
                4, 'Fair');
            """,
            legacyAttributes);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Empires (
                Id, Name, LegacyRaceId,
                CivilizationProfile_Militancy, CivilizationProfile_Determination, CivilizationProfile_RacialTolerance,
                CivilizationProfile_Progressiveness, CivilizationProfile_Loyalty, CivilizationProfile_SocialCohesion,
                CivilizationProfile_TechLevel, CivilizationProfile_Art, CivilizationProfile_Individualism, CivilizationProfile_SpatialAge,
                Founding_Origin, Founding_FoundingWorldId, Founding_FoundingColonyId, Founding_ParentEmpireId, Founding_FoundingRaceId, Founding_FoundedCentury,
                ExpansionPolicy, EconomicPowerMcr, MilitaryPower, TradeBonusMcr, Planets, CaptivePlanets, Moons,
                SubjugatedPlanets, SubjugatedMoons, IndependentColonies, SpaceHabitats,
                NativePopulationMillions, CaptivePopulationMillions, SubjectPopulationMillions, IndependentPopulationMillions,
                MilitaryForces_Personnel_CrewRating, MilitaryForces_Personnel_CrewQuality,
                MilitaryForces_Personnel_TroopRating, MilitaryForces_Personnel_TroopQuality,
                MilitaryForces_Personnel_ConscriptionPolicy,
                MilitaryForces_NavyDoctrine_FighterEmphasisPercent, MilitaryForces_NavyDoctrine_MissileEmphasisPercent,
                MilitaryForces_NavyDoctrine_BeamWeaponEmphasisPercent, MilitaryForces_NavyDoctrine_AssaultEmphasisPercent,
                MilitaryForces_NavyDoctrine_DefenseEmphasisPercent, MilitaryForces_Notes)
            VALUES (
                1, 'Legacy Empire', 1,
                0, 0, 0,
                0, 0, 0,
                6, 0, 0, 0,
                'NativeHomeworld', 101, NULL, NULL, 1, NULL,
                'CanExpand', 1500, 900, 250, 4, 0, 1,
                0, 0, 0, 0,
                3200, 0, 0, 0,
                0, 'Average',
                0, 'Average',
                'Volunteer',
                20, 20,
                20, 20,
                20, '');
            """);
    }

    private static StarWinDbContext CreateSqliteDbContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new StarWinDbContext(options);
    }

    private static StarWinDbContext CreateSqlServerDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlServer(connectionString)
            .Options;

        return new StarWinDbContext(options);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $tableName LIMIT 1";
        command.Parameters.AddWithValue("$tableName", tableName);
        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateTempFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"starwin-migration-{Guid.NewGuid():N}{extension}");
    }

    private static void DeleteIfExists(string path)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
