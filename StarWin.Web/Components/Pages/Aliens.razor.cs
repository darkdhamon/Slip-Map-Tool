using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Globalization;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Aliens : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 30;
    private const int ComboAllFilterId = -1;
    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected IStarWinImageService ImageService { get; set; } = default!;
    [Inject] protected IStarWinEntityNameService EntityNameService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected ILogger<Aliens> Logger { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    [SupplyParameterFromQuery(Name = "raceId")]
    public int? RequestedRaceId { get; set; }

    [SupplyParameterFromQuery(Name = "empireId")]
    public int? RequestedEmpireId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected int selectedRaceId;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string raceQuery = string.Empty;
    protected string raceEnvironment = string.Empty;
    protected string raceAppearance = string.Empty;
    protected string raceMaxPointCost = string.Empty;
    protected string raceStarWinTechLevel = string.Empty;
    protected string raceGurpsTechLevel = string.Empty;
    protected bool raceRequireSuperscience;
    protected bool raceHasMoreRecords;
    protected bool showRaceFilters;
    protected ExplorerAlienRaceDetail? selectedRaceDetail;
    protected ExplorerAlienRaceFilterOptions raceFilterOptions = new([], [], [], []);

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoading;
    private int lastLoadedRaceImageTargetId;
    private bool raceListLoading;
    private bool raceDetailLoading;
    private string imageUploadStatus = string.Empty;
    private ElementReference raceLoadMoreElement;
    private DotNetObjectReference<Aliens>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool raceObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int loadedRaceSectorId;
    private int loadedRaceDetailId;
    private readonly List<ExplorerAlienRaceListItem> loadedRaceSummaries = [];

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerAlienRaceListItem> LoadedRaceSummaries => loadedRaceSummaries;

    protected override async Task OnInitializedAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page OnInitializedAsync start. requestedSectorId={RequestedSectorId} requestedSystemId={RequestedSystemId} requestedRaceId={RequestedRaceId}",
            RequestedSectorId,
            RequestedSystemId,
            RequestedRaceId);

        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadRacePageAsync(resetList: true);

        Logger.LogInformation(
            "Aliens page OnInitializedAsync complete in {ElapsedMs}ms. selectedSectorId={SelectedSectorId} selectedSystemId={SelectedSystemId} selectedRaceId={SelectedRaceId} loadedRaceCount={LoadedRaceCount}",
            stopwatch.ElapsedMilliseconds,
            selectedSectorId,
            selectedSystemId,
            selectedRaceId,
            loadedRaceSummaries.Count);
    }

    protected override async Task OnParametersSetAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page OnParametersSetAsync start. requestedSectorId={RequestedSectorId} requestedSystemId={RequestedSystemId} requestedRaceId={RequestedRaceId} currentSectorId={SelectedSectorId} currentRaceId={SelectedRaceId}",
            RequestedSectorId,
            RequestedSystemId,
            RequestedRaceId,
            selectedSectorId,
            selectedRaceId);

        if (ExplorerSectors.Count == 0)
        {
            Logger.LogInformation("Aliens page OnParametersSetAsync skipped because explorer shell has no sectors.");
            return;
        }

        var requestedSectorId = RequestedSectorId ?? selectedSectorId;
        if (requestedSectorId != selectedSectorId && ExplorerSectors.Any(sector => sector.Id == requestedSectorId))
        {
            selectedSectorId = requestedSectorId;
            await LoadRacePageAsync(resetList: true);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await EnsureSelectedRaceSummaryVisibleAsync();
        await EnsureSelectedRaceDetailAsync();

        Logger.LogInformation(
            "Aliens page OnParametersSetAsync complete in {ElapsedMs}ms. selectedSectorId={SelectedSectorId} selectedSystemId={SelectedSystemId} selectedRaceId={SelectedRaceId}",
            stopwatch.ElapsedMilliseconds,
            selectedSectorId,
            selectedSystemId,
            selectedRaceId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Logger.LogDebug(
            "Aliens page OnAfterRenderAsync invoked. firstRender={FirstRender} selectedSectorId={SelectedSectorId} selectedRaceId={SelectedRaceId}",
            firstRender,
            selectedSectorId,
            selectedRaceId);

        if (firstRender)
        {
            await RestoreExplorerSessionAsync();
            browserSessionReady = true;
            StateHasChanged();
            return;
        }

        await ConfigureRaceObserverAsync();
        _ = EnsureEntityImagesLoadedAsync(backgroundLoad: true);
    }

    protected StarWinSector GetSelectedSector()
    {
        return ExplorerSectors.FirstOrDefault(item => item.Id == selectedSectorId) ?? explorerContext.CurrentSector;
    }

    protected string BuildSectionRoute(string sectionName)
    {
        return SectorExplorerRoutes.BuildSectionUri(sectionName, selectedSectorId);
    }

    protected static string DisplayResultType(StarWinSearchResultType type)
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
        ClearRaceFilters();
        await LoadRacePageAsync(resetList: true);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: selectedRaceId));
    }

    protected Task HandleSelectedSystemTextChangedAsync(string value)
    {
        selectedSystemText = value;
        var sector = GetSelectedSector();
        var systemId = ParseComboId(selectedSystemText);
        if (systemId != ComboAllFilterId && sector.Systems.Any(system => system.Id == systemId))
        {
            selectedSystemId = systemId;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: selectedRaceId));
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

    protected void SelectRace(int raceId)
    {
        selectedRaceId = raceId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId), replace: true);
    }

    protected void NavigateToWorld(int worldId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, selectedSystemId, worldId: worldId, raceId: selectedRaceId));
    }

    protected void NavigateToEmpire(int empireId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, raceId: selectedRaceId, empireId: empireId));
    }

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await RefreshExplorerDataAsync();
        selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
        await LoadRacePageAsync(resetList: true);
    }

    protected void ResetRaceWindow()
    {
        raceObserverConfigured = false;
    }

    protected async Task HandleRaceFiltersChangedAsync()
    {
        ResetRaceWindow();
        await LoadRacePageAsync(resetList: true);
    }

    protected void ToggleRaceFilters()
    {
        showRaceFilters = !showRaceFilters;
    }

    protected void ClearRaceFilters()
    {
        raceQuery = string.Empty;
        raceEnvironment = string.Empty;
        raceAppearance = string.Empty;
        raceMaxPointCost = string.Empty;
        raceStarWinTechLevel = string.Empty;
        raceGurpsTechLevel = string.Empty;
        raceRequireSuperscience = false;
        showRaceFilters = false;
        ResetRaceWindow();
    }

    [JSInvokable]
    public Task LoadMoreRaces()
    {
        raceObserverConfigured = false;
        return LoadMoreRaceSummariesAsync();
    }

    protected IReadOnlyList<EntityImage> GetEntityImages(EntityImageTargetKind targetKind, int targetId)
    {
        return entityImages
            .Where(image => image.TargetKind == targetKind && image.TargetId == targetId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.UploadedAt)
            .ToList();
    }

    private async Task ConfigureRaceObserverAsync()
    {
        if (!raceHasMoreRecords)
        {
            raceObserverConfigured = false;
            if (loadMoreScrollModule is not null)
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "race");
            }

            return;
        }

        if (raceObserverConfigured)
        {
            return;
        }

        loadMoreScrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/timelineInfiniteScroll.js");
        dotNetReference ??= DotNetObjectReference.Create(this);
        await loadMoreScrollModule.InvokeVoidAsync("observeLoadMore", "race", raceLoadMoreElement, dotNetReference, "LoadMoreRaces");
        raceObserverConfigured = true;
    }

    private async Task EnsureEntityImagesLoadedAsync(bool backgroundLoad = false)
    {
        var raceId = selectedRaceDetail?.Race.Id ?? 0;
        if (raceId <= 0)
        {
            entityImages = [];
            lastLoadedRaceImageTargetId = 0;
            return;
        }

        if (entityImagesLoading || lastLoadedRaceImageTargetId == raceId)
        {
            return;
        }

        entityImagesLoading = true;
        try
        {
            entityImages = await ImageService.GetImagesAsync(
                [new EntityImageTarget(EntityImageTargetKind.AlienRace, raceId)]);
            lastLoadedRaceImageTargetId = raceId;
            explorerRenderError = string.Empty;
            if (backgroundLoad)
            {
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                StateHasChanged();
            }
        }
        finally
        {
            entityImagesLoading = false;
        }
    }

    private async Task RefreshExplorerDataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page RefreshExplorerDataAsync start. preferredSectorId={PreferredSectorId}",
            RequestedSectorId ?? selectedSectorId);

        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: RequestedSectorId ?? selectedSectorId,
            cancellationToken: cancellationToken);

        Logger.LogInformation(
            "Aliens page RefreshExplorerDataAsync complete in {ElapsedMs}ms. sectorCount={SectorCount} currentSectorId={CurrentSectorId}",
            stopwatch.ElapsedMilliseconds,
            explorerContext.Sectors.Count,
            explorerContext.CurrentSector.Id);
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
        await LoadRacePageAsync(resetList: true);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: selectedRaceId), replace: true);
    }

    private async Task LoadRacePageAsync(bool resetList, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page LoadRacePageAsync start. resetList={ResetList} selectedSectorId={SelectedSectorId} loadedRaceSectorId={LoadedRaceSectorId} requestedRaceId={RequestedRaceId} selectedRaceId={SelectedRaceId}",
            resetList,
            selectedSectorId,
            loadedRaceSectorId,
            RequestedRaceId,
            selectedRaceId);

        if (selectedSectorId <= 0)
        {
            loadedRaceSummaries.Clear();
            raceHasMoreRecords = false;
            ClearSelectedRaceDetail();
            raceFilterOptions = new([], [], [], []);
            Logger.LogInformation("Aliens page LoadRacePageAsync reset to empty state because selectedSectorId <= 0.");
            return;
        }

        if (resetList || loadedRaceSectorId != selectedSectorId)
        {
            loadedRaceSummaries.Clear();
            loadedRaceSectorId = selectedSectorId;
            raceHasMoreRecords = false;
            raceObserverConfigured = false;
            ClearSelectedRaceDetail();
            raceFilterOptions = await ExplorerQueryService.LoadAlienRaceFilterOptionsAsync(selectedSectorId, cancellationToken);
            await LoadMoreRaceSummariesAsync(cancellationToken);
        }

        await EnsureSelectedRaceSummaryVisibleAsync(cancellationToken);
        await EnsureSelectedRaceDetailAsync(cancellationToken);

        Logger.LogInformation(
            "Aliens page LoadRacePageAsync complete in {ElapsedMs}ms. selectedSectorId={SelectedSectorId} loadedRaceCount={LoadedRaceCount} hasMore={HasMore} selectedRaceId={SelectedRaceId} detailRaceId={DetailRaceId}",
            stopwatch.ElapsedMilliseconds,
            selectedSectorId,
            loadedRaceSummaries.Count,
            raceHasMoreRecords,
            selectedRaceId,
            selectedRaceDetail?.Race.Id ?? 0);
    }

    private async Task LoadMoreRaceSummariesAsync(CancellationToken cancellationToken = default)
    {
        if (raceListLoading || selectedSectorId <= 0)
        {
            Logger.LogDebug(
                "Aliens page LoadMoreRaceSummariesAsync skipped. raceListLoading={RaceListLoading} selectedSectorId={SelectedSectorId}",
                raceListLoading,
                selectedSectorId);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page LoadMoreRaceSummariesAsync start. selectedSectorId={SelectedSectorId} offset={Offset} selectedRaceId={SelectedRaceId}",
            selectedSectorId,
            loadedRaceSummaries.Count,
            selectedRaceId);

        raceListLoading = true;
        try
        {
            var page = await ExplorerQueryService.LoadAlienRaceListPageAsync(
                BuildRaceListRequest(loadedRaceSummaries.Count),
                cancellationToken);

            foreach (var item in page.Items)
            {
                if (loadedRaceSummaries.All(existing => existing.RaceId != item.RaceId))
                {
                    loadedRaceSummaries.Add(item);
                }
            }

            raceHasMoreRecords = page.HasMore;
            await InvokeAsync(StateHasChanged);

            Logger.LogInformation(
                "Aliens page LoadMoreRaceSummariesAsync complete in {ElapsedMs}ms. loadedRaceCount={LoadedRaceCount} hasMore={HasMore} selectedRaceId={SelectedRaceId}",
                stopwatch.ElapsedMilliseconds,
                loadedRaceSummaries.Count,
                raceHasMoreRecords,
                selectedRaceId);
        }
        finally
        {
            raceListLoading = false;
        }
    }

    private async Task EnsureSelectedRaceSummaryVisibleAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveRaceFilters())
        {
            Logger.LogDebug("Aliens page EnsureSelectedRaceSummaryVisibleAsync skipped because filters are active.");
            return;
        }

        var requestedRaceId = RequestedRaceId ?? selectedRaceId;
        if (requestedRaceId <= 0 || loadedRaceSummaries.Any(item => item.RaceId == requestedRaceId))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page EnsureSelectedRaceSummaryVisibleAsync loading missing requested race summary. selectedSectorId={SelectedSectorId} requestedRaceId={RequestedRaceId}",
            selectedSectorId,
            requestedRaceId);

        var requestedItem = await ExplorerQueryService.LoadAlienRaceListItemAsync(selectedSectorId, requestedRaceId, cancellationToken);
        if (requestedItem is null)
        {
            Logger.LogInformation(
                "Aliens page EnsureSelectedRaceSummaryVisibleAsync could not find requested race summary. selectedSectorId={SelectedSectorId} requestedRaceId={RequestedRaceId} elapsedMs={ElapsedMs}",
                selectedSectorId,
                requestedRaceId,
                stopwatch.ElapsedMilliseconds);
            return;
        }

        loadedRaceSummaries.Add(requestedItem);
        loadedRaceSummaries.Sort((left, right) =>
        {
            var nameComparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0 ? nameComparison : left.RaceId.CompareTo(right.RaceId);
        });

        Logger.LogInformation(
            "Aliens page EnsureSelectedRaceSummaryVisibleAsync added missing requested race summary in {ElapsedMs}ms. selectedSectorId={SelectedSectorId} requestedRaceId={RequestedRaceId} loadedRaceCount={LoadedRaceCount}",
            stopwatch.ElapsedMilliseconds,
            selectedSectorId,
            requestedRaceId,
            loadedRaceSummaries.Count);
    }

    private async Task EnsureSelectedRaceDetailAsync(CancellationToken cancellationToken = default)
    {
        var targetRaceId = HasActiveRaceFilters()
            ? selectedRaceId
            : RequestedRaceId ?? selectedRaceId;
        if (targetRaceId <= 0)
        {
            ClearSelectedRaceDetail();
            Logger.LogDebug("Aliens page EnsureSelectedRaceDetailAsync cleared detail because no race target is available.");
            return;
        }

        if (selectedRaceDetail?.Race.Id == targetRaceId && loadedRaceDetailId == targetRaceId)
        {
            selectedRaceId = targetRaceId;
            Logger.LogDebug(
                "Aliens page EnsureSelectedRaceDetailAsync skipped because target race detail is already loaded. selectedSectorId={SelectedSectorId} targetRaceId={TargetRaceId}",
                selectedSectorId,
                targetRaceId);
            return;
        }

        if (raceDetailLoading)
        {
            Logger.LogDebug(
                "Aliens page EnsureSelectedRaceDetailAsync skipped because detail load is already in progress. selectedSectorId={SelectedSectorId} targetRaceId={TargetRaceId}",
                selectedSectorId,
                targetRaceId);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Logger.LogInformation(
            "Aliens page EnsureSelectedRaceDetailAsync start. selectedSectorId={SelectedSectorId} targetRaceId={TargetRaceId} requestedRaceId={RequestedRaceId} selectedRaceId={SelectedRaceId}",
            selectedSectorId,
            targetRaceId,
            RequestedRaceId,
            selectedRaceId);

        raceDetailLoading = true;
        try
        {
            var previousRaceId = selectedRaceDetail?.Race.Id ?? 0;
            var detail = await ExplorerQueryService.LoadAlienRaceDetailAsync(selectedSectorId, targetRaceId, cancellationToken);
            selectedRaceDetail = detail;
            selectedRaceId = detail?.Race.Id ?? 0;
            loadedRaceDetailId = selectedRaceId;
            if (previousRaceId != selectedRaceId)
            {
                entityImages = [];
                lastLoadedRaceImageTargetId = 0;
            }

            Logger.LogInformation(
                "Aliens page EnsureSelectedRaceDetailAsync complete in {ElapsedMs}ms. selectedSectorId={SelectedSectorId} selectedRaceId={SelectedRaceId} detailLoaded={DetailLoaded}",
                stopwatch.ElapsedMilliseconds,
                selectedSectorId,
                selectedRaceId,
                selectedRaceDetail is not null);
        }
        finally
        {
            raceDetailLoading = false;
        }
    }

    private void ClearSelectedRaceDetail()
    {
        selectedRaceId = 0;
        selectedRaceDetail = null;
        loadedRaceDetailId = 0;
        entityImages = [];
        lastLoadedRaceImageTargetId = 0;
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
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Aliens")));
    }

    private async Task UploadEntityImage(EntityImageTargetKind targetKind, int targetId, InputFileChangeEventArgs args)
    {
        var file = args.File;
        try
        {
            await using var stream = file.OpenReadStream(10 * 1024 * 1024);
            await ImageService.UploadImageAsync(targetKind, targetId, file.Name, file.ContentType, stream);
            lastLoadedRaceImageTargetId = 0;
            await EnsureEntityImagesLoadedAsync();
            imageUploadStatus = $"{file.Name} uploaded.";
        }
        catch (Exception exception)
        {
            imageUploadStatus = exception.Message;
        }
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
            return ComboAllFilterId;
        }

        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        var idText = separatorIndex < 0 ? value : value[..separatorIndex];
        return int.TryParse(idText, out var id) ? id : ComboAllFilterId;
    }

    private bool HasActiveRaceFilters()
    {
        return !string.IsNullOrWhiteSpace(raceQuery)
            || !string.IsNullOrWhiteSpace(raceEnvironment)
            || !string.IsNullOrWhiteSpace(raceAppearance)
            || !string.IsNullOrWhiteSpace(raceMaxPointCost)
            || !string.IsNullOrWhiteSpace(raceStarWinTechLevel)
            || !string.IsNullOrWhiteSpace(raceGurpsTechLevel)
            || raceRequireSuperscience;
    }

    private ExplorerAlienRaceListPageRequest BuildRaceListRequest(int offset)
    {
        byte? starWinTechLevel = null;
        if (byte.TryParse(raceStarWinTechLevel, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStarWinTechLevel))
        {
            starWinTechLevel = parsedStarWinTechLevel;
        }

        int? maxTotalPointCost = null;
        if (int.TryParse(raceMaxPointCost, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMaxTotalPointCost))
        {
            maxTotalPointCost = parsedMaxTotalPointCost;
        }

        return new ExplorerAlienRaceListPageRequest(
            selectedSectorId,
            offset,
            ExplorerListBatchSize,
            string.IsNullOrWhiteSpace(raceQuery) ? null : raceQuery.Trim(),
            string.IsNullOrWhiteSpace(raceEnvironment) ? null : raceEnvironment.Trim(),
            string.IsNullOrWhiteSpace(raceAppearance) ? null : raceAppearance.Trim(),
            maxTotalPointCost,
            starWinTechLevel,
            string.IsNullOrWhiteSpace(raceGurpsTechLevel) ? null : raceGurpsTechLevel.Trim(),
            raceRequireSuperscience);
    }

    public async ValueTask DisposeAsync()
    {
        if (loadMoreScrollModule is not null)
        {
            try
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "race");
                await loadMoreScrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }
}
