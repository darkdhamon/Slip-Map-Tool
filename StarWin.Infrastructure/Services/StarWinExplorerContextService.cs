using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerContextService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinExplorerContextService
{
    public async Task<StarWinExplorerContext> LoadShellAsync(
        int? preferredSectorId = null,
        bool includeReferenceData = false,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await StarWinExplorerContextLoader.LoadShellAsync(
            dbContext,
            preferredSectorId,
            includeReferenceData,
            cancellationToken);
    }
}
