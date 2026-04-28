using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExplorerContextService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinExplorerContextService
{
    public Task<StarWinExplorerContext> LoadShellAsync(
        bool includeSavedRoutes = true,
        bool includeReferenceData = true,
        int? detailedSectorId = null,
        ExplorerSectorLoadSections detailedSectorSections = ExplorerSectorLoadSections.None,
        CancellationToken cancellationToken = default)
    {
        return LoadShellAsync(
            preferredSectorId: detailedSectorId,
            includeSavedRoutes: includeSavedRoutes,
            includeReferenceData: includeReferenceData,
            cancellationToken: cancellationToken);
    }

    public async Task<StarWinExplorerContext> LoadShellAsync(
        int? preferredSectorId = null,
        bool includeSavedRoutes = false,
        bool includeReferenceData = false,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await StarWinExplorerContextLoader.LoadShellAsync(
            dbContext,
            preferredSectorId,
            includeSavedRoutes,
            includeReferenceData,
            cancellationToken);
    }
}
