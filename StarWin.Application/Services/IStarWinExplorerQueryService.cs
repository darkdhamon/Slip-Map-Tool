namespace StarWin.Application.Services;

public interface IStarWinExplorerQueryService
{
    Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> LoadTimelineEventTypesAsync(int sectorId, CancellationToken cancellationToken = default);
    Task<ExplorerTimelinePage> LoadTimelinePageAsync(ExplorerTimelinePageRequest request, CancellationToken cancellationToken = default);
    Task<ExplorerTimelineEventDetail?> LoadTimelineEventDetailAsync(int eventId, CancellationToken cancellationToken = default);
}

public sealed record ExplorerLookupOption(int Id, string Name);

public sealed record ExplorerSectorOverviewData(
    int SectorId,
    int SystemCount,
    int WorldCount,
    int ColonyCount,
    int EmpireCount,
    int RaceCount,
    IReadOnlyList<ExplorerLookupOption> Systems,
    IReadOnlyList<ExplorerLookupOption> Empires);

public sealed record ExplorerTimelinePageRequest(
    int SectorId,
    int Offset,
    int Limit,
    string? EventType = null);

public sealed record ExplorerTimelinePage(
    IReadOnlyList<ExplorerTimelineListItem> Items,
    bool HasMore);

public sealed record ExplorerTimelineListItem(
    int EventId,
    string Title,
    string EventType,
    string TimeLabel,
    int Century,
    int? RaceId,
    int? OtherRaceId,
    int? EmpireId,
    int? ColonyId,
    int? WorldId,
    int? SystemId);

public sealed record ExplorerTimelineEventDetail(
    int EventId,
    string Title,
    string EventType,
    string TimeLabel,
    int Century,
    string Description,
    string? ImportDataJson,
    ExplorerLookupOption? Race,
    ExplorerLookupOption? OtherRace,
    ExplorerLookupOption? Empire,
    ExplorerLookupOption? Colony,
    ExplorerLookupOption? World,
    ExplorerLookupOption? System);
