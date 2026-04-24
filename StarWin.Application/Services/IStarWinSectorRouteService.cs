using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;

namespace StarWin.Application.Services;

public interface IStarWinSectorRouteService
{
    Task<SectorRouteSaveResult> SaveCurrentRoutesAsync(
        int sectorId,
        IProgress<SectorRouteSaveProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<SectorSavedRoute> SaveManualRouteAsync(
        SectorManualRouteSaveRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteSavedRouteAsync(
        int sectorId,
        int routeId,
        CancellationToken cancellationToken = default);
}

public sealed record SectorRouteSaveResult(
    int SectorId,
    int RouteCount,
    int GeneratedRouteCount,
    int PreservedUserRouteCount,
    bool ReplacedExistingRoutes,
    DateTime GeneratedAtUtc,
    SectorHyperlaneNetworkReport NetworkReport);

public sealed record SectorManualRouteSaveRequest(
    int SectorId,
    int? RouteId,
    int SourceSystemId,
    int TargetSystemId,
    decimal DistanceParsecs,
    decimal TravelTimeYears,
    byte TechnologyLevel,
    string TierName,
    int? PrimaryOwnerEmpireId,
    string PrimaryOwnerEmpireName,
    int? SecondaryOwnerEmpireId,
    string SecondaryOwnerEmpireName,
    bool IsUserPersisted);

public sealed record SectorRouteSaveProgress(
    string Status,
    string Detail,
    int Percent,
    int? ProcessedItems = null,
    int? TotalItems = null);
