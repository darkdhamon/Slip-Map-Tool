using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinSectorRouteService
{
    Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(
        int sectorId,
        CancellationToken cancellationToken = default);
}

public sealed record SectorRouteSaveResult(
    int SectorId,
    int RouteCount,
    bool ReplacedExistingRoutes,
    DateTime GeneratedAtUtc);
