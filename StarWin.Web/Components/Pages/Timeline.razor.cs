using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Timeline : ComponentBase
{
    private const string TimelineLoadingTimerFunction = "starforgedAtlasNavigation.getSectionLoadingStartedAt";
    private const string ClearTimelineLoadingTimerFunction = "starforgedAtlasNavigation.clearSectionLoadingStartedAt";
    private static readonly IReadOnlyList<string> timelineLoadingSteps =
    [
        "Resolve the selected sector context.",
        "Load the first timeline event page.",
        "Render the chronology and detail workspace."
    ];

    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    private readonly Dictionary<int, ExplorerSectorEntityUsage> sectorEntityUsageById = [];

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];

    private bool timelinePageLoadingVisible = true;
    private bool initialTimelineLoadCompleted;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private long timelinePageLoadingStartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<AlienRace> ExplorerAlienRaces => explorerContext.AlienRaces;
    protected IReadOnlyList<Empire> ExplorerEmpires => explorerContext.Empires;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        await EnsureSectorEntityUsageLoadedAsync(selectedSectorId);
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        timelinePageLoadingVisible = true;
        initialTimelineLoadCompleted = false;
        timelinePageLoadingStartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ExplorerSectors.Count == 0)
        {
            return;
        }

        var requestedSectorId = RequestedSectorId ?? selectedSectorId;
        if (requestedSectorId != selectedSectorId && ExplorerSectors.Any(sector => sector.Id == requestedSectorId))
        {
            selectedSectorId = requestedSectorId;
            await EnsureSectorEntityUsageLoadedAsync(selectedSectorId);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await RestoreExplorerSessionAsync();
        await RestoreTimelineLoadingTimerAsync();
        browserSessionReady = true;
        StateHasChanged();
    }

    protected StarWinSector GetSelectedSector()
    {
        return ExplorerSectors.FirstOrDefault(item => item.Id == selectedSectorId) ?? explorerContext.CurrentSector;
    }

    protected string BuildSectionRoute(string sectionName)
    {
        return SectorExplorerRoutes.BuildSectionUri(sectionName, selectedSectorId);
    }

    protected string DisplayResultType(StarWinSearchResultType type)
    {
        return type switch
        {
            StarWinSearchResultType.StarSystem => "System",
            StarWinSearchResultType.World => "World",
            StarWinSearchResultType.AlienRace => "Race",
            StarWinSearchResultType.Empire => "Empire",
            StarWinSearchResultType.Colony => "Colony",
            StarWinSearchResultType.SpaceHabitat => "Habitat",
            StarWinSearchResultType.History => "History",
            _ => type.ToString()
        };
    }

    protected async Task HandleSectorChangedAsync(int sectorId)
    {
        selectedSectorId = sectorId;
        await EnsureSectorEntityUsageLoadedAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Timeline", selectedSectorId, selectedSystemId));
    }

    protected Task HandleSelectedSystemTextChangedAsync(string value)
    {
        selectedSystemText = value;
        var sector = GetSelectedSector();
        var systemId = ParseComboId(selectedSystemText);
        if (systemId > 0 && sector.Systems.Any(system => system.Id == systemId))
        {
            selectedSystemId = systemId;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Timeline", selectedSectorId, selectedSystemId), replace: true);
        }

        return PersistExplorerSessionAsync();
    }

    protected Task HandleSearchQueryChangedAsync(string value)
    {
        searchQuery = value;
        return RunSearchAsync();
    }

    protected void NavigateToSearchResult(StarWinSearchResult result)
    {
        var targetUri = SectorExplorerRoutes.BuildSectionUri(
            result.Tab,
            result.SectorId ?? selectedSectorId,
            result.SystemId ?? 0,
            result.WorldId ?? 0,
            result.ColonyId ?? 0,
            result.SpaceHabitatId ?? 0,
            result.RaceId ?? 0,
            result.EmpireId ?? 0);

        NavigationManager.NavigateTo(targetUri);
    }

    protected Task NavigateToRace(int raceId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId));
        return Task.CompletedTask;
    }

    protected Task NavigateToEmpire(int empireId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: empireId));
        return Task.CompletedTask;
    }

    protected Task NavigateToColony(ExplorerTimelineLinkTarget target)
    {
        if (target.ColonyId > 0)
        {
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri(
                "Colonies",
                selectedSectorId,
                target.SystemId > 0 ? target.SystemId : selectedSystemId,
                target.WorldId,
                target.ColonyId));
        }

        return Task.CompletedTask;
    }

    protected Task NavigateToWorld(ExplorerTimelineLinkTarget target)
    {
        if (target.WorldId > 0)
        {
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri(
                "Worlds",
                selectedSectorId,
                target.SystemId > 0 ? target.SystemId : selectedSystemId,
                worldId: target.WorldId));
        }

        return Task.CompletedTask;
    }

    protected Task NavigateToSystemRecord(int systemId)
    {
        if (systemId > 0)
        {
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, systemId));
        }

        return Task.CompletedTask;
    }

    protected async Task HandleTimelineLoadingStateChangedAsync(bool isLoading)
    {
        if (initialTimelineLoadCompleted || isLoading)
        {
            return;
        }

        initialTimelineLoadCompleted = true;
        timelinePageLoadingVisible = false;
        await ClearTimelineLoadingTimerAsync();
        StateHasChanged();
    }

    private async Task RestoreTimelineLoadingTimerAsync()
    {
        long? preservedStartedAtUnixMs;
        try
        {
            preservedStartedAtUnixMs = await JS.InvokeAsync<long?>(TimelineLoadingTimerFunction);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        catch (JSException)
        {
            return;
        }

        if (preservedStartedAtUnixMs is > 0)
        {
            timelinePageLoadingStartedAtUnixMs = preservedStartedAtUnixMs.Value;
        }
    }

    private async Task ClearTimelineLoadingTimerAsync()
    {
        try
        {
            await JS.InvokeVoidAsync(ClearTimelineLoadingTimerFunction);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }
    }

    private async Task RefreshExplorerDataAsync(CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: RequestedSectorId ?? selectedSectorId,
            includeReferenceData: true,
            cancellationToken: cancellationToken);

        sectorEntityUsageById.Clear();
    }

    private async Task EnsureSectorEntityUsageLoadedAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return;
        }

        if (sectorEntityUsageById.ContainsKey(sectorId))
        {
            return;
        }

        sectorEntityUsageById[sectorId] = await ExplorerQueryService.LoadSectorEntityUsageAsync(sectorId, cancellationToken);
    }

    private async Task RunSearchAsync()
    {
        searchResults = await ExplorerQueryService.SearchSectorAsync(selectedSectorId, searchQuery);
    }

    private async Task RestoreExplorerSessionAsync()
    {
        if (browserSessionRestored)
        {
            return;
        }

        browserSessionRestored = true;
        var storedSelection = await ExplorerPageState.RestoreSelectionAsync(JS, RequestedSectorId);
        if (storedSelection is null)
        {
            return;
        }

        var sector = ExplorerSectors.FirstOrDefault(item => item.Id == storedSelection.SectorId);
        if (sector is null)
        {
            return;
        }

        selectedSectorId = sector.Id;
        await EnsureSectorEntityUsageLoadedAsync(selectedSectorId);
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Timeline", selectedSectorId, selectedSystemId), replace: true);
    }

    private async Task PersistExplorerSessionAsync()
    {
        await ExplorerPageState.PersistSelectionAsync(
            JS,
            browserSessionReady,
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Timeline")));
    }

    private static string FormatSelectedSystem(StarWinSector sector, int systemId)
    {
        var system = sector.Systems.FirstOrDefault(item => item.Id == systemId);
        return system is null ? string.Empty : $"{system.Id} - {system.Name}";
    }

    private static int ParseComboId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        var idText = separatorIndex < 0 ? value : value[..separatorIndex];
        return int.TryParse(idText, out var id) ? id : 0;
    }

}
