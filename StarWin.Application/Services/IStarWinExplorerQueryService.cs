namespace StarWin.Application.Services;

public interface IStarWinExplorerQueryService
{
    Task<ExplorerSectorOverviewData> LoadSectorOverviewAsync(int sectorId, CancellationToken cancellationToken = default);
    Task<ExplorerAlienRaceFilterOptions> LoadAlienRaceFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default);
    Task<ExplorerAlienRaceListPage> LoadAlienRaceListPageAsync(ExplorerAlienRaceListPageRequest request, CancellationToken cancellationToken = default);
    Task<ExplorerAlienRaceListItem?> LoadAlienRaceListItemAsync(int sectorId, int raceId, CancellationToken cancellationToken = default);
    Task<ExplorerAlienRaceDetail?> LoadAlienRaceDetailAsync(int sectorId, int raceId, CancellationToken cancellationToken = default);
    Task<ExplorerEmpireFilterOptions> LoadEmpireFilterOptionsAsync(int sectorId, CancellationToken cancellationToken = default);
    Task<ExplorerEmpireListPage> LoadEmpireListPageAsync(ExplorerEmpireListPageRequest request, CancellationToken cancellationToken = default);
    Task<ExplorerEmpireListItem?> LoadEmpireListItemAsync(int sectorId, int empireId, CancellationToken cancellationToken = default);
    Task<ExplorerEmpireDetail?> LoadEmpireDetailAsync(int sectorId, int empireId, CancellationToken cancellationToken = default);
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

public sealed record ExplorerAlienRaceFilterOptions(
    IReadOnlyList<string> EnvironmentTypes,
    IReadOnlyList<string> AppearanceTypes,
    IReadOnlyList<int> StarWinTechLevels,
    IReadOnlyList<string> GurpsTechLevels);

public sealed record ExplorerAlienRaceListPageRequest(
    int SectorId,
    int Offset,
    int Limit,
    string? Query = null,
    string? EnvironmentType = null,
    string? AppearanceType = null,
    int? MaxTotalPointCost = null,
    byte? StarWinTechLevel = null,
    string? GurpsTechLevel = null,
    bool RequireSuperscience = false);

public sealed record ExplorerAlienRaceListPage(
    IReadOnlyList<ExplorerAlienRaceListItem> Items,
    bool HasMore);

public sealed record ExplorerAlienRaceListItem(
    int RaceId,
    string Name,
    string AppearanceType,
    string EnvironmentType);

public sealed record ExplorerAlienRaceDetail(
    int SectorId,
    StarWin.Domain.Model.Entity.Civilization.AlienRace Race,
    StarWin.Domain.Model.Entity.StarMap.World? HomeWorld,
    IReadOnlyList<StarWin.Domain.Model.Entity.Civilization.Empire> Empires);

public sealed record ExplorerEmpireFilterOptions(
    IReadOnlyList<ExplorerLookupOption> Races);

public sealed record ExplorerEmpireListPageRequest(
    int SectorId,
    int Offset,
    int Limit,
    string? Query = null,
    int? RaceId = null);

public sealed record ExplorerEmpireListPage(
    IReadOnlyList<ExplorerEmpireListItem> Items,
    bool HasMore);

public sealed record ExplorerEmpireListItem(
    int EmpireId,
    string Name,
    int Planets,
    int GurpsTechLevel,
    bool IsFallen);

public sealed record ExplorerEmpireDetail(
    int SectorId,
    StarWin.Domain.Model.Entity.Civilization.Empire Empire,
    StarWin.Domain.Model.Entity.StarMap.World? HomeWorld,
    IReadOnlyList<ExplorerEmpireRaceMembershipDetail> MemberRaces,
    IReadOnlyList<ExplorerEmpireColonyListing> Colonies,
    int ControlledColonyCount,
    bool IsFallen);

public sealed record ExplorerEmpireRaceMembershipDetail(
    int RaceId,
    string RaceName,
    StarWin.Domain.Model.Entity.Civilization.EmpireRaceRole Role,
    long PopulationMillions,
    bool IsPrimary);

public sealed record ExplorerEmpireColonyListing(
    int ColonyId,
    string ColonyName,
    long EstimatedPopulation,
    bool IsControlled,
    int WorldId,
    string WorldName,
    int SystemId,
    string SystemName);

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
