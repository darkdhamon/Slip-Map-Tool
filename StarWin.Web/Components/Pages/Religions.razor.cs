using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Religions : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 30;
    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    [SupplyParameterFromQuery(Name = "religionId")]
    public int? RequestedReligionId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected int selectedReligionId;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string religionQuery = string.Empty;
    protected string religionTypeText = string.Empty;
    protected bool religionHasMoreRecords;
    protected bool showReligionFilters;
    protected bool religionListLoading;
    protected bool religionDetailLoading;
    protected ExplorerReligionDetail? selectedReligionDetail;
    protected ExplorerReligionFilterOptions religionFilterOptions = new([]);

    private ElementReference religionLoadMoreElement;
    private DotNetObjectReference<Religions>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool religionObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int loadedReligionSectorId;
    private readonly List<ExplorerReligionListItem> loadedReligionSummaries = [];

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerReligionListItem> LoadedReligionSummaries => loadedReligionSummaries;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadReligionPageAsync(resetList: true);
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
            await LoadReligionPageAsync(resetList: true);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await EnsureSelectedReligionDetailAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RestoreExplorerSessionAsync();
            browserSessionReady = true;
            StateHasChanged();
            return;
        }

        await ConfigureReligionObserverAsync();
    }

    protected StarWinSector GetSelectedSector()
    {
        return ExplorerSectors.FirstOrDefault(item => item.Id == selectedSectorId) ?? explorerContext.CurrentSector;
    }

    protected string BuildSectionRoute(string sectionName)
    {
        return SectorExplorerRoutes.BuildSectionUri(sectionName, selectedSectorId, selectedSystemId, religionId: selectedReligionId);
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
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        ClearReligionFilters();
        await LoadReligionPageAsync(resetList: true);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Religions", selectedSectorId, selectedSystemId, religionId: selectedReligionId));
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Religions", selectedSectorId, selectedSystemId, religionId: selectedReligionId));
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

    protected void SelectReligion(int religionId)
    {
        selectedReligionId = religionId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Religions", selectedSectorId, selectedSystemId, religionId: religionId), replace: true);
    }

    protected void NavigateToEmpire(int empireId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: empireId, religionId: selectedReligionId));
    }

    protected void NavigateToRace(int raceId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId, religionId: selectedReligionId));
    }

    protected void ToggleReligionFilters()
    {
        showReligionFilters = !showReligionFilters;
    }

    protected async Task HandleReligionFiltersChangedAsync()
    {
        ResetReligionWindow();
        await LoadReligionPageAsync(resetList: true);
    }

    protected async Task ApplyReligionTypeFilterAsync()
    {
        ResetReligionWindow();
        await LoadReligionPageAsync(resetList: true);
    }

    protected void ClearReligionFilters()
    {
        religionQuery = string.Empty;
        religionTypeText = string.Empty;
        showReligionFilters = false;
        ResetReligionWindow();
    }

    protected async Task HandleClearReligionFiltersAsync()
    {
        ClearReligionFilters();
        await LoadReligionPageAsync(resetList: true);
    }

    [JSInvokable]
    public Task LoadMoreReligions()
    {
        religionObserverConfigured = false;
        return LoadMoreReligionSummariesAsync();
    }

    protected string GetReligionSummary(ExplorerReligionDetail detail)
    {
        var sourceLabel = detail.Religion.IsUserDefined ? "custom" : "imported";
        return $"{detail.Religion.Type} tradition tracked across {detail.Empires.Count} empire{(detail.Empires.Count == 1 ? string.Empty : "s")} and {detail.Races.Count} species in this sector ({sourceLabel}).";
    }

    private void ResetReligionWindow()
    {
        religionObserverConfigured = false;
    }

    private async Task ConfigureReligionObserverAsync()
    {
        if (!religionHasMoreRecords)
        {
            religionObserverConfigured = false;
            if (loadMoreScrollModule is not null)
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "religion");
            }

            return;
        }

        if (religionObserverConfigured)
        {
            return;
        }

        loadMoreScrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/timelineInfiniteScroll.js");
        dotNetReference ??= DotNetObjectReference.Create(this);
        await loadMoreScrollModule.InvokeVoidAsync("observeLoadMore", "religion", religionLoadMoreElement, dotNetReference, "LoadMoreReligions");
        religionObserverConfigured = true;
    }

    private async Task RefreshExplorerDataAsync(CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: RequestedSectorId ?? selectedSectorId,
            includeSavedRoutes: false,
            includeReferenceData: false,
            cancellationToken: cancellationToken);
    }

    private async Task RunSearchAsync()
    {
        searchResults = await ExplorerQueryService.SearchSectorAsync(selectedSectorId, searchQuery);
    }

    private async Task RestoreExplorerSessionAsync()
    {
        if (browserSessionRestored || RequestedSectorId is not null)
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
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await LoadReligionPageAsync(resetList: true);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Religions", selectedSectorId, selectedSystemId, religionId: selectedReligionId), replace: true);
    }

    private async Task PersistExplorerSessionAsync()
    {
        if (!browserSessionReady)
        {
            return;
        }

        await ExplorerPageState.PersistSelectionAsync(
            JS,
            browserSessionReady,
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Religions")));
    }

    private async Task LoadReligionPageAsync(bool resetList, CancellationToken cancellationToken = default)
    {
        if (selectedSectorId <= 0)
        {
            loadedReligionSummaries.Clear();
            religionHasMoreRecords = false;
            selectedReligionId = 0;
            selectedReligionDetail = null;
            religionFilterOptions = new([]);
            return;
        }

        if (resetList || loadedReligionSectorId != selectedSectorId)
        {
            loadedReligionSummaries.Clear();
            loadedReligionSectorId = selectedSectorId;
            religionHasMoreRecords = false;
            religionObserverConfigured = false;
            religionFilterOptions = await ExplorerQueryService.LoadReligionFilterOptionsAsync(selectedSectorId, cancellationToken);
            await LoadMoreReligionSummariesAsync(cancellationToken);
        }

        await EnsureSelectedReligionSummaryVisibleAsync(cancellationToken);
        await EnsureSelectedReligionDetailAsync(cancellationToken);
    }

    private async Task LoadMoreReligionSummariesAsync(CancellationToken cancellationToken = default)
    {
        if (religionListLoading || selectedSectorId <= 0)
        {
            return;
        }

        religionListLoading = true;
        try
        {
            var page = await ExplorerQueryService.LoadReligionListPageAsync(
                new ExplorerReligionListPageRequest(
                    selectedSectorId,
                    loadedReligionSummaries.Count,
                    ExplorerListBatchSize,
                    string.IsNullOrWhiteSpace(religionQuery) ? null : religionQuery.Trim(),
                    string.IsNullOrWhiteSpace(religionTypeText) ? null : religionTypeText.Trim()),
                cancellationToken);

            foreach (var item in page.Items)
            {
                if (loadedReligionSummaries.All(existing => existing.ReligionId != item.ReligionId))
                {
                    loadedReligionSummaries.Add(item);
                }
            }

            religionHasMoreRecords = page.HasMore;
            if (selectedReligionId == 0 && loadedReligionSummaries.Count > 0)
            {
                selectedReligionId = loadedReligionSummaries[0].ReligionId;
            }
            else if (selectedReligionId > 0 && loadedReligionSummaries.All(item => item.ReligionId != selectedReligionId))
            {
                selectedReligionId = loadedReligionSummaries.FirstOrDefault()?.ReligionId ?? 0;
            }

            await EnsureSelectedReligionSummaryVisibleAsync(cancellationToken);
            await EnsureSelectedReligionDetailAsync(cancellationToken);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            religionListLoading = false;
        }
    }

    private async Task EnsureSelectedReligionSummaryVisibleAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveReligionFilters())
        {
            return;
        }

        var targetReligionId = RequestedReligionId ?? selectedReligionId;
        if (targetReligionId <= 0 || loadedReligionSummaries.Any(item => item.ReligionId == targetReligionId))
        {
            return;
        }

        var requestedItem = await ExplorerQueryService.LoadReligionListItemAsync(selectedSectorId, targetReligionId, cancellationToken);
        if (requestedItem is null)
        {
            return;
        }

        loadedReligionSummaries.Add(requestedItem);
        loadedReligionSummaries.Sort((left, right) =>
        {
            var nameComparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0 ? nameComparison : left.ReligionId.CompareTo(right.ReligionId);
        });
    }

    private async Task EnsureSelectedReligionDetailAsync(CancellationToken cancellationToken = default)
    {
        var targetReligionId = HasActiveReligionFilters()
            ? selectedReligionId
            : RequestedReligionId ?? selectedReligionId;
        if (targetReligionId <= 0)
        {
            targetReligionId = loadedReligionSummaries.FirstOrDefault()?.ReligionId ?? 0;
        }

        if (targetReligionId <= 0)
        {
            selectedReligionId = 0;
            selectedReligionDetail = null;
            return;
        }

        if (religionDetailLoading)
        {
            return;
        }

        religionDetailLoading = true;
        try
        {
            var detail = await ExplorerQueryService.LoadReligionDetailAsync(selectedSectorId, targetReligionId, cancellationToken);
            if (detail is null && loadedReligionSummaries.Count > 0)
            {
                targetReligionId = loadedReligionSummaries[0].ReligionId;
                detail = await ExplorerQueryService.LoadReligionDetailAsync(selectedSectorId, targetReligionId, cancellationToken);
            }

            selectedReligionDetail = detail;
            selectedReligionId = detail?.Religion.Id ?? 0;
        }
        finally
        {
            religionDetailLoading = false;
        }
    }

    private bool HasActiveReligionFilters()
    {
        return !string.IsNullOrWhiteSpace(religionQuery)
            || !string.IsNullOrWhiteSpace(religionTypeText);
    }

    protected static string FormatSelectedSystem(StarWinSector sector, int systemId)
    {
        var system = sector.Systems.FirstOrDefault(item => item.Id == systemId);
        return system is null ? string.Empty : $"{system.Id} - {system.Name}";
    }

    protected static int ParseComboId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return -1;
        }

        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        var idText = separatorIndex < 0 ? value : value[..separatorIndex];
        return int.TryParse(idText, out var id) ? id : -1;
    }

    protected static string FormatPopulationMillions(long populationMillions)
    {
        const decimal oneMillion = 1_000_000m;
        const decimal oneBillion = 1_000_000_000m;
        const decimal oneTrillion = 1_000_000_000_000m;

        var absolutePopulation = populationMillions * oneMillion;
        if (absolutePopulation >= oneTrillion)
        {
            return $"{Math.Round(absolutePopulation / oneTrillion, MidpointRounding.AwayFromZero):N0} trillion";
        }

        if (absolutePopulation >= oneBillion)
        {
            return $"{Math.Round(absolutePopulation / oneBillion, MidpointRounding.AwayFromZero):N0} billion";
        }

        return $"{Math.Round(absolutePopulation / oneMillion, MidpointRounding.AwayFromZero):N0} million";
    }

    protected static string GetPopulationTooltip(long populationMillions)
    {
        var absolutePopulation = populationMillions * 1_000_000m;
        return $"{absolutePopulation:N0}";
    }

    protected static int GetGurpsTechLevel(byte starWinTechLevel)
    {
        return GurpsTechnologyLevelMapper.GetBaseTechLevel(starWinTechLevel);
    }

    public async ValueTask DisposeAsync()
    {
        if (loadMoreScrollModule is not null)
        {
            try
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "religion");
                await loadMoreScrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }
}
