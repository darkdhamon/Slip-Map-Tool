using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinSectorEmpireStatsService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinSectorEmpireStatsService
{
    public async Task<SectorEmpireStatsRefreshResult> RebuildSectorStatsAsync(
        int sectorId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await SectorEmpireStatRefreshOperations.RebuildSectorAsync(dbContext, sectorId, cancellationToken);
    }

    public async Task<SectorEmpireStatRefreshResult?> RefreshEmpireStatsAsync(
        int sectorId,
        int empireId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await SectorEmpireStatRefreshOperations.RefreshEmpireAsync(dbContext, sectorId, empireId, cancellationToken);
    }

    public async Task MarkSectorStatsStaleAsync(
        int sectorId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await SectorEmpireStatRefreshOperations.MarkSectorStatsStaleAsync(dbContext, sectorId, cancellationToken);
    }
}
