using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerContextService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinExplorerContextService
{
    public async Task<StarWinExplorerContext> LoadShellAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await StarWinExplorerContextLoader.LoadShellAsync(dbContext, cancellationToken);
    }

    public async Task<StarWinSector?> LoadSectorAsync(int sectorId, bool includeHistory = false, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await StarWinExplorerContextLoader.LoadSectorAsync(dbContext, sectorId, includeHistory, cancellationToken);
    }
}
