using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;
using StarWin.Infrastructure.Services;
using Xunit;

namespace StarWin.Infrastructure.Tests.AppConfiguration;

public sealed class StarWinAppConfigurationServiceTests
{
    [Fact]
    public async Task ResetDatabaseAsync_recreates_schema_and_removes_existing_data()
    {
        var databasePath = CreateTempFilePath(".db");

        try
        {
            using var serviceProvider = BuildServiceProvider(databasePath);

            await using (var migrationContext = serviceProvider.GetRequiredService<IDbContextFactory<StarWinDbContext>>().CreateDbContext())
            {
                await migrationContext.Database.EnsureCreatedAsync();
            }

            await using (var seedContext = serviceProvider.GetRequiredService<IDbContextFactory<StarWinDbContext>>().CreateDbContext())
            {
                seedContext.Sectors.Add(new StarWinSector { Id = 1, Name = "Delcora" });
                await seedContext.SaveChangesAsync();
            }

            var service = new StarWinAppConfigurationService(
                serviceProvider.GetRequiredService<IDbContextFactory<StarWinDbContext>>(),
                serviceProvider);
            await service.ResetDatabaseAsync();

            await using var verificationContext = serviceProvider.GetRequiredService<IDbContextFactory<StarWinDbContext>>().CreateDbContext();
            Assert.Equal(0, await verificationContext.Sectors.CountAsync());
            Assert.True(await verificationContext.Database.CanConnectAsync());
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

    private static string CreateTempFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"starwin-config-{Guid.NewGuid():N}{extension}");
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
