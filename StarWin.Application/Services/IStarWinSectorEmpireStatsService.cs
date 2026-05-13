namespace StarWin.Application.Services;

public interface IStarWinSectorEmpireStatsService
{
    Task<SectorEmpireStatsRefreshResult> RebuildSectorStatsAsync(
        int sectorId,
        CancellationToken cancellationToken = default);

    Task<SectorEmpireStatRefreshResult?> RefreshEmpireStatsAsync(
        int sectorId,
        int empireId,
        CancellationToken cancellationToken = default);

    Task MarkSectorStatsStaleAsync(
        int sectorId,
        CancellationToken cancellationToken = default);
}

public sealed record SectorEmpireStatsRefreshResult(
    int SectorId,
    int EmpireCount,
    DateTime CalculatedAtUtc);

public sealed record SectorEmpireStatRefreshResult(
    int SectorId,
    int EmpireId,
    int ControlledWorldCount,
    int TrackedWorldCount,
    DateTime CalculatedAtUtc);
