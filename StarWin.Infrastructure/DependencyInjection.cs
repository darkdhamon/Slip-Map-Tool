using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using StarWin.Application.Services.LegacyImport;
using StarWin.Application.Services;
using StarWin.Infrastructure.Data;
using StarWin.Infrastructure.Services;

namespace StarWin.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStarWinInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["StarWin:DatabaseProvider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("StarWin")
            ?? throw new InvalidOperationException("The StarWin connection string is not configured.");

        void ConfigureDatabase(DbContextOptionsBuilder options)
        {
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                EnsureSqliteDirectoryExists(connectionString);
                options.UseSqlite(connectionString);
                return;
            }

            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
                return;
            }

            throw new InvalidOperationException($"Unsupported StarWin database provider '{provider}'.");
        }

        services.AddDbContext<StarWinDbContext>(
            ConfigureDatabase,
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<StarWinDbContext>(ConfigureDatabase);

        services.AddSingleton<IStarWinWorkspace, StarWinDatabaseWorkspace>();
        services.AddScoped<IStarWinAppConfigurationService, StarWinAppConfigurationService>();
        services.AddScoped<IStarWinExplorerContextService, StarWinExplorerContextService>();
        services.AddScoped<IStarWinExplorerQueryService, StarWinExplorerQueryService>();
        services.AddScoped<IStarWinSearchService, StarWinSearchService>();
        services.AddScoped<IStarWinImageService, StarWinImageService>();
        services.AddScoped<IStarWinEntityNoteService, StarWinEntityNoteService>();
        services.AddScoped<IStarWinEntityNameService, StarWinEntityNameService>();
        services.AddScoped<IStarWinIndependentColonyService, StarWinIndependentColonyService>();
        services.AddScoped<IStarWinSpaceHabitatService, StarWinSpaceHabitatService>();
        services.AddScoped<IStarWinLegacyImportService, StarWinLegacyImportService>();
        services.AddScoped<IStarWinSectorConfigurationService, StarWinSectorConfigurationService>();
        services.AddScoped<IStarWinSectorRouteService, StarWinSectorRouteService>();

        return services;
    }

    public static async Task SeedStarWinDevelopmentDataAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StarWinDbContext>();
        await StarWinDevelopmentSeeder.SeedAsync(dbContext);
    }

    public static async Task MigrateStarWinDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StarWinDbContext>();

        if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            await UpgradeSqliteDatabaseAsync(dbContext);
            await StarSystemNameUniqueness.EnsureUniquePersistedNamesAsync(dbContext);
            await dbContext.Database.MigrateAsync();
            await BackfillAlienEmpireImportParityDataAsync(dbContext);
            return;
        }

        await StarSystemNameUniqueness.EnsureUniquePersistedNamesAsync(dbContext);
        await dbContext.Database.MigrateAsync();
        await BackfillAlienEmpireImportParityDataAsync(dbContext);
    }

    private static async Task UpgradeSqliteDatabaseAsync(StarWinDbContext dbContext)
    {
        var connectionString = dbContext.Database.GetConnectionString();
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var databasePath = builder.DataSource;
        if (string.IsNullOrWhiteSpace(databasePath) || databasePath.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        databasePath = Path.GetFullPath(databasePath);
        if (!File.Exists(databasePath))
        {
            return;
        }

        await using var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync();

        if (await TableExistsAsync(connection, "__EFMigrationsHistory"))
        {
            return;
        }

        if (!await TableExistsAsync(connection, "Sectors"))
        {
            return;
        }

        var backupPath = await CreateSqliteBackupAsync(databasePath);

        try
        {
            await EnsureColumnAsync(connection, "SectorConfigurations", "OffLaneMaximumDistanceParsecs", "REAL NOT NULL DEFAULT 2");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl9AndBelowMaximumConnectionsPerSystem", "INTEGER NOT NULL DEFAULT 4");
            await EnsureColumnAsync(connection, "SectorConfigurations", "AdditionalCrossEmpireConnectionsPerSystem", "INTEGER NOT NULL DEFAULT 1");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl6HyperlaneName", "TEXT NOT NULL DEFAULT 'Basic Hyperlane'");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl6MaximumDistanceParsecs", "REAL NOT NULL DEFAULT 1");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl6OffLaneSpeedMultiplier", "REAL NOT NULL DEFAULT 2");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl6HyperlaneSpeedModifier", "REAL NOT NULL DEFAULT 2");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl7HyperlaneName", "TEXT NOT NULL DEFAULT 'Enhanced Hyperlane'");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl7MaximumDistanceParsecs", "REAL NOT NULL DEFAULT 1.2");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl7OffLaneSpeedMultiplier", "REAL NOT NULL DEFAULT 4");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl7HyperlaneSpeedModifier", "REAL NOT NULL DEFAULT 2.25");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl8HyperlaneName", "TEXT NOT NULL DEFAULT 'Advanced Hyperlane'");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl8MaximumDistanceParsecs", "REAL NOT NULL DEFAULT 1.4");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl8OffLaneSpeedMultiplier", "REAL NOT NULL DEFAULT 8");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl8HyperlaneSpeedModifier", "REAL NOT NULL DEFAULT 2.5");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl9HyperlaneName", "TEXT NOT NULL DEFAULT 'Prime Hyperlane'");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl9MaximumDistanceParsecs", "REAL NOT NULL DEFAULT 1.6");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl9OffLaneSpeedMultiplier", "REAL NOT NULL DEFAULT 16");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl9HyperlaneSpeedModifier", "REAL NOT NULL DEFAULT 2.75");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl10HyperlaneName", "TEXT NOT NULL DEFAULT 'Ascendant Hyperlane'");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl10MaximumDistanceParsecs", "REAL NOT NULL DEFAULT -1");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl10OffLaneSpeedMultiplier", "REAL NOT NULL DEFAULT 32");
            await EnsureColumnAsync(connection, "SectorConfigurations", "Tl10HyperlaneSpeedModifier", "REAL NOT NULL DEFAULT 3");

            if (await ColumnExistsAsync(connection, "SectorConfigurations", "BasicHyperlaneMaximumLengthParsecs"))
            {
                await ExecuteNonQueryAsync(
                    connection,
                    """
                    UPDATE SectorConfigurations
                    SET Tl6MaximumDistanceParsecs = COALESCE(Tl6MaximumDistanceParsecs, BasicHyperlaneMaximumLengthParsecs),
                        Tl7MaximumDistanceParsecs = COALESCE(Tl7MaximumDistanceParsecs, OwnedHyperlaneBaseMaximumLengthParsecs)
                    """);
            }

            await EnsureAlienEmpireImportParitySqliteAsync(connection);

            await ExecuteNonQueryAsync(
                connection,
                """
                CREATE TABLE IF NOT EXISTS "SectorSavedRoutes" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_SectorSavedRoutes" PRIMARY KEY AUTOINCREMENT,
                    "SectorId" INTEGER NOT NULL,
                    "SourceSystemId" INTEGER NOT NULL,
                    "TargetSystemId" INTEGER NOT NULL,
                    "DistanceParsecs" TEXT NOT NULL,
                    "TravelTimeYears" TEXT NOT NULL,
                    "TechnologyLevel" INTEGER NOT NULL,
                    "TierName" TEXT NOT NULL,
                    "PrimaryOwnerEmpireId" INTEGER NULL,
                    "PrimaryOwnerEmpireName" TEXT NOT NULL,
                    "SecondaryOwnerEmpireId" INTEGER NULL,
                    "SecondaryOwnerEmpireName" TEXT NOT NULL,
                    "GeneratedAtUtc" TEXT NOT NULL,
                    CONSTRAINT "FK_SectorSavedRoutes_Sectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES "Sectors" ("Id") ON DELETE CASCADE
                )
                """);

            await ExecuteNonQueryAsync(
                connection,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SectorSavedRoutes_SectorId_SourceSystemId_TargetSystemId"
                ON "SectorSavedRoutes" ("SectorId", "SourceSystemId", "TargetSystemId")
                """);

            await ExecuteNonQueryAsync(
                connection,
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                )
                """);

            var productVersion = typeof(DbContext).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?.Split('+', 2)[0]
                ?? "10.0.6";

            foreach (var migrationId in dbContext.Database.GetMigrations())
            {
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ($migrationId, $productVersion)
                    """;
                command.Parameters.AddWithValue("$migrationId", migrationId);
                command.Parameters.AddWithValue("$productVersion", productVersion);
                await command.ExecuteNonQueryAsync();
            }
        }
        catch
        {
            File.Copy(backupPath, databasePath, overwrite: true);
            throw;
        }
    }

    private static async Task EnsureAlienEmpireImportParitySqliteAsync(SqliteConnection connection)
    {
        await EnsureColumnAsync(connection, "Empires", "GovernmentType", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "Empires", "ImportDataJson", "TEXT NULL");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_Art", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_Determination", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_Individualism", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_Loyalty", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_Militancy", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_Progressiveness", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_RacialTolerance", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "Empires", "CivilizationModifiers_SocialCohesion", "INTEGER NOT NULL DEFAULT 0");

        await EnsureColumnAsync(connection, "AlienRaces", "Abilities", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "AtmosphereBreathed", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "BodyCharacteristics", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_Art", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_Determination", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_Individualism", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_Loyalty", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_Militancy", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_Progressiveness", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_RacialTolerance", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "CivilizationProfile_SocialCohesion", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "ColorPattern", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "Colors", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "EyeCharacteristics", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "EyeColors", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "GravityPreference", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "HairColors", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "HairType", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "ImportDataJson", "TEXT NULL");
        await EnsureColumnAsync(connection, "AlienRaces", "LimbTypes", "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnAsync(connection, "AlienRaces", "RequiresUserRename", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(connection, "AlienRaces", "TemperaturePreference", "TEXT NOT NULL DEFAULT ''");

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE TABLE IF NOT EXISTS "Religions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Religions" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "IsUserDefined" INTEGER NOT NULL DEFAULT 0
            )
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE TABLE IF NOT EXISTS "EmpireReligions" (
                "EmpireId" INTEGER NOT NULL,
                "ReligionId" INTEGER NOT NULL,
                "ReligionName" TEXT NOT NULL,
                "PopulationPercent" REAL NOT NULL,
                CONSTRAINT "PK_EmpireReligions" PRIMARY KEY ("EmpireId", "ReligionId"),
                CONSTRAINT "FK_EmpireReligions_Empires_EmpireId" FOREIGN KEY ("EmpireId") REFERENCES "Empires" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_EmpireReligions_Religions_ReligionId" FOREIGN KEY ("ReligionId") REFERENCES "Religions" ("Id") ON DELETE CASCADE
            )
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Religions_Name"
            ON "Religions" ("Name")
            """);

        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE INDEX IF NOT EXISTS "IX_EmpireReligions_ReligionId"
            ON "EmpireReligions" ("ReligionId")
            """);

        if (await ColumnExistsAsync(connection, "AlienRaces", "GovernmentType"))
        {
            await ExecuteNonQueryAsync(
                connection,
                """
                UPDATE "Empires"
                SET "GovernmentType" = COALESCE(NULLIF("GovernmentType", ''), (
                    SELECT "GovernmentType"
                    FROM "AlienRaces"
                    WHERE "AlienRaces"."Id" = "Empires"."LegacyRaceId"
                ))
                WHERE ("GovernmentType" IS NULL OR TRIM("GovernmentType") = '')
                  AND "LegacyRaceId" IS NOT NULL;
                """);
        }

        if (await ColumnExistsAsync(connection, "AlienRaces", "Religion"))
        {
            await ExecuteNonQueryAsync(
                connection,
                """
                INSERT OR IGNORE INTO "Religions" ("Name", "Type", "IsUserDefined")
                SELECT DISTINCT TRIM("Religion"), TRIM("Religion"), 0
                FROM "AlienRaces"
                WHERE "Religion" IS NOT NULL AND TRIM("Religion") <> '';
                """);

            await ExecuteNonQueryAsync(
                connection,
                """
                INSERT OR IGNORE INTO "EmpireReligions" ("EmpireId", "ReligionId", "ReligionName", "PopulationPercent")
                SELECT empire."Id", religion."Id", religion."Name", 100
                FROM "Empires" AS empire
                INNER JOIN "AlienRaces" AS race ON race."Id" = empire."LegacyRaceId"
                INNER JOIN "Religions" AS religion ON religion."Name" = TRIM(race."Religion")
                WHERE race."Religion" IS NOT NULL AND TRIM(race."Religion") <> '';
                """);
        }
    }

    private static async Task BackfillAlienEmpireImportParityDataAsync(StarWinDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        await NormalizeLegacyAlienEnumValuesAsync(dbContext, cancellationToken);

        dbContext.ChangeTracker.Clear();

        var races = await dbContext.AlienRaces
            .OrderBy(race => race.Id)
            .ToListAsync(cancellationToken);
        var empires = await dbContext.Empires
            .OrderBy(empire => empire.Id)
            .ToListAsync(cancellationToken);

        var racesById = races.ToDictionary(race => race.Id);
        var changed = false;

        foreach (var race in races)
        {
            changed |= FillMissingCivilizationProfile(race);
        }

        foreach (var empire in empires)
        {
            if (empire.LegacyRaceId is not int legacyRaceId
                || !racesById.TryGetValue(legacyRaceId, out var race)
                || race.ImportDataJson is not null
                || string.IsNullOrWhiteSpace(race.HairType)
                || !string.IsNullOrWhiteSpace(empire.GovernmentType))
            {
                continue;
            }

            empire.GovernmentType = race.HairType;
            race.HairType = string.Empty;
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool FillMissingCivilizationProfile(StarWin.Domain.Model.Entity.Civilization.AlienRace race)
    {
        var changed = false;
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.Militancy, GetLegacyAttribute(race.LegacyAttributes, 1), value => race.CivilizationProfile.Militancy = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.Determination, GetLegacyAttribute(race.LegacyAttributes, 2), value => race.CivilizationProfile.Determination = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.RacialTolerance, GetLegacyAttribute(race.LegacyAttributes, 3), value => race.CivilizationProfile.RacialTolerance = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.Progressiveness, GetLegacyAttribute(race.LegacyAttributes, 4), value => race.CivilizationProfile.Progressiveness = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.Loyalty, GetLegacyAttribute(race.LegacyAttributes, 5), value => race.CivilizationProfile.Loyalty = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.SocialCohesion, GetLegacyAttribute(race.LegacyAttributes, 6), value => race.CivilizationProfile.SocialCohesion = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.Art, GetLegacyAttribute(race.LegacyAttributes, 13), value => race.CivilizationProfile.Art = value);
        changed |= FillMissingCivilizationValue(race.CivilizationProfile.Individualism, GetLegacyAttribute(race.LegacyAttributes, 14), value => race.CivilizationProfile.Individualism = value);
        return changed;
    }

    private static bool FillMissingCivilizationValue(byte currentValue, byte importedValue, Action<byte> assignValue)
    {
        if (currentValue != 0 || importedValue == 0)
        {
            return false;
        }

        assignValue(importedValue);
        return true;
    }

    private static Task NormalizeLegacyAlienEnumValuesAsync(StarWinDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE AlienRaces
            SET DevotionLevel = CASE TRIM(DevotionLevel)
                WHEN 'Moderate' THEN 'Fair'
                WHEN 'Low' THEN 'Poor'
                ELSE DevotionLevel
            END,
            BiologyProfile_PsiRating = CASE TRIM(BiologyProfile_PsiRating)
                WHEN 'Very Poor' THEN 'VeryPoor'
                ELSE BiologyProfile_PsiRating
            END
            WHERE DevotionLevel IN ('Moderate', 'Low')
               OR BiologyProfile_PsiRating = 'Very Poor';
            """,
            cancellationToken);
    }

    private static byte GetLegacyAttribute(IReadOnlyList<byte>? attributes, int oneBasedIndex)
    {
        return attributes is not null && oneBasedIndex >= 1 && oneBasedIndex <= attributes.Count
            ? attributes[oneBasedIndex - 1]
            : (byte)0;
    }

    private static async Task<string> CreateSqliteBackupAsync(string databasePath)
    {
        var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "Backups");
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(
            backupDirectory,
            $"{Path.GetFileNameWithoutExtension(databasePath)}-{DateTime.UtcNow:yyyyMMddHHmmss}.bak{Path.GetExtension(databasePath)}");
        await using var source = File.Open(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var destination = File.Create(backupPath);
        await source.CopyToAsync(destination);
        return backupPath;
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $tableName LIMIT 1";
        command.Parameters.AddWithValue("$tableName", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is not null;
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

    private static async Task EnsureColumnAsync(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        if (await ColumnExistsAsync(connection, tableName, columnName))
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition}");
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) ||
            builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var databasePath = Path.GetFullPath(builder.DataSource);
        var databaseDirectory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }
    }
}
