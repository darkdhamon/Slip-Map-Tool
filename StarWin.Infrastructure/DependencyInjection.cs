using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<StarWinDbContext>(ConfigureDatabase);

        services.AddSingleton<IStarWinWorkspace, StarWinDatabaseWorkspace>();
        services.AddScoped<IStarWinSearchService, StarWinSearchService>();
        services.AddScoped<IStarWinImageService, StarWinImageService>();
        services.AddScoped<IStarWinEntityNoteService, StarWinEntityNoteService>();
        services.AddScoped<IStarWinEntityNameService, StarWinEntityNameService>();
        services.AddScoped<IStarWinIndependentColonyService, StarWinIndependentColonyService>();
        services.AddScoped<IStarWinSpaceHabitatService, StarWinSpaceHabitatService>();
        services.AddScoped<IStarWinLegacyImportService, StarWinLegacyImportService>();
        services.AddScoped<IStarWinSectorConfigurationService, StarWinSectorConfigurationService>();

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
            await dbContext.Database.EnsureCreatedAsync();
            return;
        }

        await dbContext.Database.MigrateAsync();
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
