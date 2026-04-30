using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Empires : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 30;
    private const int ComboAllFilterId = -1;
    private const int EmpireFilterDebounceMilliseconds = 250;
    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected IStarWinImageService ImageService { get; set; } = default!;
    [Inject] protected IStarWinEntityNameService EntityNameService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    [SupplyParameterFromQuery(Name = "empireId")]
    public int? RequestedEmpireId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected int selectedEmpireId;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string empireQuery = string.Empty;
    protected string empireRaceText = string.Empty;
    protected string empireMemberRaceSearch = string.Empty;
    protected string empireColonySearch = string.Empty;
    protected string empireRelationshipSearch = string.Empty;
    protected string empireRelationshipType = string.Empty;
    protected int empireRaceId = ComboAllFilterId;
    protected bool showOnlyFallenEmpires;
    protected bool empireHasMoreRecords;
    protected bool empireListLoading;
    protected bool empireDetailLoading;
    protected bool empireFilterPending;
    protected bool empireFilterReloadRunning;
    protected ExplorerEmpireDetail? selectedEmpireDetail;
    protected ExplorerEmpireFilterOptions empireFilterOptions = new([]);

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoading;
    private int lastLoadedEmpireImageTargetId;
    private string imageUploadStatus = string.Empty;
    private ElementReference empireLoadMoreElement;
    private DotNetObjectReference<Empires>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool empireObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int loadedEmpireSectorId;
    private int empireFilterRequestVersion;
    private bool empireFilterReloadQueued;
    private CancellationTokenSource? empireFilterDebounceCancellation;
    private readonly List<ExplorerEmpireListItem> loadedEmpireSummaries = [];
    private int loadedEmpireDetailId;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerEmpireListItem> LoadedEmpireSummaries => loadedEmpireSummaries;
    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadEmpirePageAsync(resetList: true);
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
            await LoadEmpirePageAsync(resetList: true);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await EnsureSelectedEmpireSummaryVisibleAsync();
        await EnsureSelectedEmpireDetailAsync();
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

        await ConfigureEmpireObserverAsync();
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
        ClearEmpireFilters();
        await LoadEmpirePageAsync(resetList: true);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: selectedEmpireId));
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: selectedEmpireId));
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

    protected void SelectEmpire(int empireId)
    {
        selectedEmpireId = empireId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: empireId), replace: true);
    }

    protected void NavigateToWorld(int worldId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, selectedSystemId, worldId: worldId, empireId: selectedEmpireId));
    }

    protected void NavigateToColony(int colonyId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, selectedSystemId, colonyId: colonyId, empireId: selectedEmpireId));
    }

    protected void NavigateToRace(int raceId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId, empireId: selectedEmpireId));
    }

    protected void NavigateToEmpire(int empireId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: empireId));
    }

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await RefreshExplorerDataAsync();
        selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
        await LoadEmpirePageAsync(resetList: true);
    }

    protected void ResetEmpireWindow()
    {
        empireObserverConfigured = false;
    }

    protected Task HandleEmpireFiltersChangedAsync()
    {
        RequestEmpireFilterReload(useDebounce: true);
        return Task.CompletedTask;
    }

    protected Task ApplyEmpireRaceFilterAsync()
    {
        empireRaceId = ParseComboId(empireRaceText);
        RequestEmpireFilterReload(useDebounce: true);
        return Task.CompletedTask;
    }

    protected Task ToggleFallenEmpireFilterAsync()
    {
        showOnlyFallenEmpires = !showOnlyFallenEmpires;
        RequestEmpireFilterReload(useDebounce: false);
        return Task.CompletedTask;
    }

    protected void ClearEmpireFilters()
    {
        empireQuery = string.Empty;
        empireRaceText = string.Empty;
        empireRaceId = ComboAllFilterId;
        showOnlyFallenEmpires = false;
        ResetEmpireWindow();
    }

    protected Task HandleClearEmpireFiltersAsync()
    {
        ClearEmpireFilters();
        RequestEmpireFilterReload(useDebounce: false);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task LoadMoreEmpires()
    {
        empireObserverConfigured = false;
        return LoadMoreEmpireSummariesAsync();
    }

    private async Task ConfigureEmpireObserverAsync()
    {
        if (!empireHasMoreRecords)
        {
            empireObserverConfigured = false;
            if (loadMoreScrollModule is not null)
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "empire");
            }

            return;
        }

        if (empireObserverConfigured)
        {
            return;
        }

        loadMoreScrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/timelineInfiniteScroll.js");
        dotNetReference ??= DotNetObjectReference.Create(this);
        await loadMoreScrollModule.InvokeVoidAsync("observeLoadMore", "empire", empireLoadMoreElement, dotNetReference, "LoadMoreEmpires");
        empireObserverConfigured = true;
    }

    private async Task EnsureEntityImagesLoadedAsync(bool backgroundLoad = false)
    {
        var empireId = selectedEmpireDetail?.Empire.Id ?? 0;
        if (empireId <= 0)
        {
            entityImages = [];
            lastLoadedEmpireImageTargetId = 0;
            return;
        }

        if (entityImagesLoading || lastLoadedEmpireImageTargetId == empireId)
        {
            return;
        }

        entityImagesLoading = true;
        try
        {
            entityImages = await ImageService.GetImagesAsync(
                [new EntityImageTarget(EntityImageTargetKind.Empire, empireId)]);
            lastLoadedEmpireImageTargetId = empireId;
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
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: RequestedSectorId ?? selectedSectorId,
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
        await LoadEmpirePageAsync(resetList: true);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: selectedEmpireId), replace: true);
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
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Empires")));
    }

    private async Task LoadEmpirePageAsync(bool resetList, CancellationToken cancellationToken = default, int? requestVersion = null)
    {
        if (selectedSectorId <= 0)
        {
            loadedEmpireSummaries.Clear();
            empireHasMoreRecords = false;
            ClearSelectedEmpireDetail();
            empireFilterOptions = new([]);
            return;
        }

        var sectorChanged = loadedEmpireSectorId != selectedSectorId;
        if (resetList || sectorChanged)
        {
            loadedEmpireSummaries.Clear();
            loadedEmpireSectorId = selectedSectorId;
            empireHasMoreRecords = false;
            empireObserverConfigured = false;
            ClearSelectedEmpireDetail();
            if (sectorChanged || empireFilterOptions.Races.Count == 0)
            {
                empireFilterOptions = await ExplorerQueryService.LoadEmpireFilterOptionsAsync(selectedSectorId, cancellationToken);
            }

            if (IsStaleEmpireFilterRequest(requestVersion))
            {
                return;
            }

            await LoadMoreEmpireSummariesAsync(cancellationToken, requestVersion);
        }

        await EnsureSelectedEmpireSummaryVisibleAsync(cancellationToken, requestVersion);
        await EnsureSelectedEmpireDetailAsync(cancellationToken, requestVersion);
    }

    private void RequestEmpireFilterReload(bool useDebounce)
    {
        var hasInFlightReload = empireFilterReloadRunning || empireListLoading;
        CancelEmpireFilterReloadDebounce();
        empireFilterRequestVersion++;
        empireFilterPending = true;
        empireFilterReloadQueued = false;
        ResetEmpireWindow();
        loadedEmpireSummaries.Clear();
        empireHasMoreRecords = false;
        if (hasInFlightReload)
        {
            empireFilterReloadQueued = true;
            _ = InvokeAsync(StateHasChanged);
            return;
        }

        if (useDebounce)
        {
            ScheduleEmpireFilterReload(empireFilterRequestVersion);
            return;
        }

        _ = RunEmpireFilterReloadAsync(empireFilterRequestVersion);
    }

    private void BeginEmpireFilterReload()
    {
        CancelEmpireFilterReloadDebounce();
        empireFilterPending = true;
        loadedEmpireSummaries.Clear();
        empireHasMoreRecords = false;
        empireObserverConfigured = false;
        empireFilterRequestVersion++;
        _ = RunEmpireFilterReloadAsync(empireFilterRequestVersion);
    }

    private void ScheduleEmpireFilterReload(int requestVersion)
    {
        CancelEmpireFilterReloadDebounce();
        var cancellation = new CancellationTokenSource();
        empireFilterDebounceCancellation = cancellation;
        _ = RunScheduledEmpireFilterReloadAsync(requestVersion, cancellation.Token);
    }

    private async Task RunScheduledEmpireFilterReloadAsync(int requestVersion, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(EmpireFilterDebounceMilliseconds, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await InvokeAsync(() => RunEmpireFilterReloadAsync(requestVersion));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelEmpireFilterReloadDebounce()
    {
        if (empireFilterDebounceCancellation is null)
        {
            return;
        }

        empireFilterDebounceCancellation.Cancel();
        empireFilterDebounceCancellation.Dispose();
        empireFilterDebounceCancellation = null;
    }

    private async Task RunEmpireFilterReloadAsync(int requestVersion)
    {
        empireFilterReloadRunning = true;
        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Delay(50);
            await InvokeAsync(() => LoadEmpirePageAsync(resetList: true, requestVersion: requestVersion));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            explorerRenderError = exception.Message;
        }
        finally
        {
            empireFilterReloadRunning = false;
            var shouldStartQueuedReload = empireFilterReloadQueued;
            if (shouldStartQueuedReload)
            {
                empireFilterReloadQueued = false;
            }

            if (requestVersion == empireFilterRequestVersion && !shouldStartQueuedReload)
            {
                empireFilterPending = false;
            }

            await InvokeAsync(StateHasChanged);
            if (shouldStartQueuedReload)
            {
                var queuedRequestVersion = empireFilterRequestVersion;
                await InvokeAsync(() => RunEmpireFilterReloadAsync(queuedRequestVersion));
            }
        }
    }

    private async Task LoadMoreEmpireSummariesAsync(CancellationToken cancellationToken = default, int? requestVersion = null)
    {
        if (empireListLoading || selectedSectorId <= 0)
        {
            return;
        }

        empireListLoading = true;
        try
        {
            var page = await ExplorerQueryService.LoadEmpireListPageAsync(
                new ExplorerEmpireListPageRequest(
                    selectedSectorId,
                    loadedEmpireSummaries.Count,
                    ExplorerListBatchSize,
                    string.IsNullOrWhiteSpace(empireQuery) ? null : empireQuery.Trim(),
                    empireRaceId == ComboAllFilterId ? null : empireRaceId,
                    showOnlyFallenEmpires),
                cancellationToken);

            if (IsStaleEmpireFilterRequest(requestVersion))
            {
                return;
            }

            foreach (var item in page.Items)
            {
                if (loadedEmpireSummaries.All(existing => existing.EmpireId != item.EmpireId))
                {
                    loadedEmpireSummaries.Add(item);
                }
            }

            empireHasMoreRecords = page.HasMore;
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            empireListLoading = false;
        }
    }

    private async Task EnsureSelectedEmpireSummaryVisibleAsync(CancellationToken cancellationToken = default, int? requestVersion = null)
    {
        if (HasActiveEmpireFilters())
        {
            return;
        }

        var targetEmpireId = RequestedEmpireId ?? selectedEmpireId;
        if (targetEmpireId <= 0 || loadedEmpireSummaries.Any(item => item.EmpireId == targetEmpireId))
        {
            return;
        }

        var requestedItem = await ExplorerQueryService.LoadEmpireListItemAsync(selectedSectorId, targetEmpireId, cancellationToken);
        if (requestedItem is null)
        {
            return;
        }

        if (IsStaleEmpireFilterRequest(requestVersion))
        {
            return;
        }

        loadedEmpireSummaries.Add(requestedItem);
        loadedEmpireSummaries.Sort((left, right) =>
        {
            var nameComparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0 ? nameComparison : left.EmpireId.CompareTo(right.EmpireId);
        });
    }

    private async Task EnsureSelectedEmpireDetailAsync(CancellationToken cancellationToken = default, int? requestVersion = default)
    {
        var targetEmpireId = HasActiveEmpireFilters()
            ? selectedEmpireId
            : RequestedEmpireId ?? selectedEmpireId;

        if (targetEmpireId <= 0)
        {
            ClearSelectedEmpireDetail();
            return;
        }

        if (selectedEmpireDetail?.Empire.Id == targetEmpireId && loadedEmpireDetailId == targetEmpireId)
        {
            selectedEmpireId = targetEmpireId;
            return;
        }

        if (empireDetailLoading)
        {
            return;
        }

        empireDetailLoading = true;
        try
        {
            var previousEmpireId = selectedEmpireDetail?.Empire.Id ?? 0;
            var detail = await ExplorerQueryService.LoadEmpireDetailAsync(selectedSectorId, targetEmpireId, cancellationToken);

            if (IsStaleEmpireFilterRequest(requestVersion))
            {
                return;
            }

            if (detail?.Empire.Id != loadedEmpireDetailId)
            {
                loadedEmpireDetailId = detail?.Empire.Id ?? 0;
                empireMemberRaceSearch = string.Empty;
                empireColonySearch = string.Empty;
            }

            selectedEmpireDetail = detail;
            selectedEmpireId = detail?.Empire.Id ?? 0;
            if (previousEmpireId != selectedEmpireId)
            {
                entityImages = [];
                lastLoadedEmpireImageTargetId = 0;
            }
        }
        finally
        {
            empireDetailLoading = false;
        }
    }

    private void ClearSelectedEmpireDetail()
    {
        selectedEmpireId = 0;
        selectedEmpireDetail = null;
        loadedEmpireDetailId = 0;
        entityImages = [];
        lastLoadedEmpireImageTargetId = 0;
    }

    protected ExplorerEmpireColonyListing? GetCapitalColony(ExplorerEmpireDetail detail)
    {
        var homeworldColony = detail.Colonies.FirstOrDefault(listing => listing.WorldId == detail.Empire.Founding.FoundingWorldId && listing.IsControlled);
        if (homeworldColony is not null)
        {
            return homeworldColony;
        }

        return detail.Colonies.FirstOrDefault(listing => listing.IsControlled) ?? detail.Colonies.FirstOrDefault();
    }

    private bool HasActiveEmpireFilters()
    {
        return !string.IsNullOrWhiteSpace(empireQuery)
            || empireRaceId != ComboAllFilterId
            || showOnlyFallenEmpires;
    }

    private bool IsStaleEmpireFilterRequest(int? requestVersion)
    {
        return requestVersion is int version
            && version != empireFilterRequestVersion;
    }

    protected IReadOnlyList<EntityImage> GetEntityImages(EntityImageTargetKind targetKind, int targetId)
    {
        return entityImages
            .Where(image => image.TargetKind == targetKind && image.TargetId == targetId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.UploadedAt)
            .ToList();
    }

    private async Task UploadEntityImage(EntityImageTargetKind targetKind, int targetId, InputFileChangeEventArgs args)
    {
        var file = args.File;
        try
        {
            await using var stream = file.OpenReadStream(10 * 1024 * 1024);
            await ImageService.UploadImageAsync(targetKind, targetId, file.Name, file.ContentType, stream);
            lastLoadedEmpireImageTargetId = 0;
            await EnsureEntityImagesLoadedAsync();
            imageUploadStatus = $"{file.Name} uploaded.";
        }
        catch (Exception exception)
        {
            imageUploadStatus = exception.Message;
        }
    }

    protected static string FormatRaceOption(AlienRace race)
    {
        return $"{race.Id} - {race.Name}";
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
            return ComboAllFilterId;
        }

        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        var idText = separatorIndex < 0 ? value : value[..separatorIndex];
        return int.TryParse(idText, out var id) ? id : ComboAllFilterId;
    }

    protected static string DisplayPopulation(long population)
    {
        return population >= 1_000_000_000
            ? $"{population / 1_000_000_000m:0.#} billion"
            : $"{population / 1_000_000m:0.#} million";
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

    protected static string DisplayPopulationPercent(decimal populationPercent)
    {
        return populationPercent <= 0m
            ? "0%"
            : $"{populationPercent:0.#}%";
    }

    protected static string DisplayModifier(int modifier)
    {
        return modifier > 0
            ? $"+{modifier}"
            : modifier.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    protected static string GetEmpireListSummary(ExplorerEmpireListItem empire)
    {
        return $"GURPS TL {empire.GurpsTechLevel}; {DescribeWorldTracking(empire.ControlledWorldCount, empire.TrackedWorldCount)}";
    }

    protected static string GetEmpireColonyRowClass(ExplorerEmpireColonyListing listing)
    {
        return listing.IsControlled
            ? "relationship-row"
            : "relationship-row conquered";
    }

    protected static string GetEmpireRelationshipRowClass(ExplorerEmpireRelationshipListing relationship)
    {
        return relationship.Relation.Trim().ToLowerInvariant() switch
        {
            "war" or "no intercourse" => "relationship-row relation-hostile",
            "alliance" or "unity" => "relationship-row relation-friendly",
            "trade" => "relationship-row relation-neutral",
            _ => "relationship-row"
        };
    }

    protected static string FormatEmpireColonyDetails(ExplorerEmpireColonyListing listing)
    {
        var details = new List<string>
        {
            listing.SystemName,
            DisplayPopulation(listing.EstimatedPopulation)
        };

        if (!listing.IsControlled)
        {
            details.Add(string.IsNullOrWhiteSpace(listing.ControllingEmpireName)
                ? "control lost; no current empire recorded"
                : $"controlled by {listing.ControllingEmpireName}");
        }

        return string.Join("; ", details);
    }

    protected static string FormatEmpireRelationshipDetails(ExplorerEmpireRelationshipListing relationship)
    {
        return relationship.Age > 0
            ? $"{relationship.Relation}; contact age {relationship.Age}"
            : relationship.Relation;
    }

    protected static IReadOnlyList<ExplorerEmpireRaceMembershipDetail> FilterMemberRaces(
        IReadOnlyList<ExplorerEmpireRaceMembershipDetail> memberRaces,
        string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return memberRaces;
        }

        var trimmedQuery = query.Trim();
        return memberRaces
            .Where(member =>
                member.RaceName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)
                || member.Role.ToString().Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    protected static IReadOnlyList<ExplorerEmpireColonyListing> FilterColonies(
        IReadOnlyList<ExplorerEmpireColonyListing> colonies,
        string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return colonies;
        }

        var trimmedQuery = query.Trim();
        return colonies
            .Where(colony =>
                colony.ColonyName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)
                || colony.WorldName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)
                || colony.SystemName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(colony.ControllingEmpireName)
                    && colony.ControllingEmpireName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    protected static IReadOnlyList<ExplorerEmpireRelationshipListing> FilterRelationships(
        IReadOnlyList<ExplorerEmpireRelationshipListing> relationships,
        string query,
        string relationshipType)
    {
        IEnumerable<ExplorerEmpireRelationshipListing> filteredRelationships = relationships;

        if (!string.IsNullOrWhiteSpace(relationshipType))
        {
            filteredRelationships = filteredRelationships.Where(relationship =>
                relationship.Relation.Equals(relationshipType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return filteredRelationships.ToList();
        }

        var trimmedQuery = query.Trim();
        return filteredRelationships
            .Where(relationship =>
                relationship.OtherEmpireName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    protected static IReadOnlyList<string> GetRelationshipTypeOptions(
        IReadOnlyList<ExplorerEmpireRelationshipListing> relationships)
    {
        return relationships
            .Select(relationship => relationship.Relation)
            .Where(relationshipType => !string.IsNullOrWhiteSpace(relationshipType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(relationshipType => relationshipType, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string DescribeWorldTracking(int controlledWorldCount, int trackedWorldCount)
    {
        if (trackedWorldCount <= 0)
        {
            return "0 worlds";
        }

        if (controlledWorldCount == trackedWorldCount)
        {
            return trackedWorldCount == 1 ? "1 world" : $"{trackedWorldCount} worlds";
        }

        return $"{controlledWorldCount} controlled / {trackedWorldCount} tracked worlds";
    }

    protected static int GetGurpsTechLevel(Empire empire)
    {
        return GurpsTechnologyLevelMapper.GetBaseTechLevel(empire.CivilizationProfile.TechLevel);
    }

    public async ValueTask DisposeAsync()
    {
        CancelEmpireFilterReloadDebounce();
        if (loadMoreScrollModule is not null)
        {
            try
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "empire");
                await loadMoreScrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }
}
