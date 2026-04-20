using Microsoft.EntityFrameworkCore;
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
        services.AddDbContext<StarWinDbContext>(
            options => options.UseSqlServer(configuration.GetConnectionString("StarWin")),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<StarWinDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("StarWin")));

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
}
