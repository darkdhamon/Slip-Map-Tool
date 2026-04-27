using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Infrastructure.Data;
using Xunit;

namespace StarWin.Infrastructure.Tests.AppConfiguration;

public sealed class StarWinDatabaseMigrationTests
{
    [Fact]
    public async Task MigrateStarWinDatabaseAsync_creates_fresh_sqlite_schema_without_sqlserver_migration_failures()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await using var serviceProvider = BuildServiceProvider(databasePath);

            await serviceProvider.MigrateStarWinDatabaseAsync();

            await using var verificationContext = serviceProvider.GetRequiredService<IDbContextFactory<StarWinDbContext>>().CreateDbContext();
            Assert.True(await verificationContext.Database.CanConnectAsync());
            Assert.True(await verificationContext.Database.EnsureCreatedAsync() is false);
            Assert.True(await TableExistsAsync(verificationContext, "Sectors"));
            Assert.True(await TableExistsAsync(verificationContext, "StarSystems"));
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task MigrateStarWinDatabaseAsync_recovers_from_partial_sqlite_migration_artifacts()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            await CreatePartialMigrationHistoryAsync(databasePath);
            await using var serviceProvider = BuildServiceProvider(databasePath);

            await serviceProvider.MigrateStarWinDatabaseAsync();

            await using var verificationContext = serviceProvider.GetRequiredService<IDbContextFactory<StarWinDbContext>>().CreateDbContext();
            Assert.True(await TableExistsAsync(verificationContext, "Sectors"));
            Assert.True(await TableExistsAsync(verificationContext, "StarSystems"));
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    private static ServiceProvider BuildServiceProvider(string databasePath)
    {
        var services = new ServiceCollection();
        void ConfigureDatabase(DbContextOptionsBuilder options)
        {
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            options.UseSqlite($"Data Source={databasePath}");
        }

        services.AddDbContext<StarWinDbContext>(
            ConfigureDatabase,
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<StarWinDbContext>(ConfigureDatabase);
        return services.BuildServiceProvider();
    }

    private static async Task<bool> TableExistsAsync(StarWinDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private static string CreateTempFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"starwin-migrate-{Guid.NewGuid():N}{extension}");
    }

    private static async Task CreatePartialMigrationHistoryAsync(string databasePath)
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );

            CREATE TABLE "__EFMigrationsLock" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
                "Timestamp" TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    private static void DeleteIfExists(string path)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
