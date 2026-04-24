using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinSectorRouteService
{
    Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(
        int sectorId,
        IProgress<SectorRouteSaveProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record SectorRouteSaveResult(
    int SectorId,
    int RouteCount,
    bool ReplacedExistingRoutes,
    DateTime GeneratedAtUtc);

public sealed record SectorRouteSaveProgress(
    string Status,
    string Detail,
    int Percent,
    int? ProcessedItems = null,
    int? TotalItems = null);
