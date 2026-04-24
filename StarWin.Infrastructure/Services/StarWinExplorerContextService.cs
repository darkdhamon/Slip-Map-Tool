using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerContextService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinExplorerContextService
{
    public async Task<StarWinExplorerContext> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await StarWinExplorerContextLoader.LoadAsync(dbContext, cancellationToken);
    }
}
