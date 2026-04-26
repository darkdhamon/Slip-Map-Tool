using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Infrastructure;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinAppConfigurationService(
    IDbContextFactory<StarWinDbContext> dbContextFactory,
    IServiceProvider serviceProvider) : IStarWinAppConfigurationService
{
    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        string? providerName;

        await using (var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            providerName = dbContext.Database.ProviderName;
            await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        }

        if (providerName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            await using var sqliteContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await sqliteContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await serviceProvider.MigrateStarWinDatabaseAsync();
    }
}
