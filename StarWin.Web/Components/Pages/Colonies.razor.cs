using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Colonies : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 120;
    private const int ComboAllFilterId = -1;

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

    [SupplyParameterFromQuery(Name = "colonyId")]
    public int? RequestedColonyId { get; set; }

    [SupplyParameterFromQuery(Name = "raceId")]
    public int? RequestedRaceId { get; set; }

    [SupplyParameterFromQuery(Name = "empireId")]
    public int? RequestedEmpireId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected ExplorerColonyFilterOptions colonyFilterOptions = new([], [], []);
    protected ExplorerColonyDetail? selectedColonyDetail;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected int selectedColonyId;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string colonyQuery = string.Empty;
    protected string colonyRaceText = string.Empty;
    protected string colonyEmpireText = string.Empty;
    protected int colonyRaceId = ComboAllFilterId;
    protected int colonyEmpireId = ComboAllFilterId;
    protected string colonyStatus = string.Empty;
    protected string colonyClass = string.Empty;
    protected bool colonyHasMoreRecords;
    protected bool showColonyFilters;
    protected string imageUploadStatus = string.Empty;

    private readonly List<ExplorerColonyListItem> loadedColonySummaries = [];
    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoading;
    private bool colonyListLoading;
    private bool colonyDetailLoading;
    private bool colonyObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int loadedColonySectorId;
    private int loadedColonyDetailId;
    private string lastLoadedImageKey = string.Empty;
    private ElementReference colonyLoadMoreElement;
    private DotNetObjectReference<Colonies>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerColonyListItem> LoadedColonySummaries => loadedColonySummaries;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerShellAsync(RequestedSectorId);
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = await ResolveRequestedSystemIdAsync(initialSector);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadColonyPageAsync(resetList: true);
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
            await LoadColonyPageAsync(resetList: true);
        }

        var sector = GetSelectedSector();
        selectedSystemId = await ResolveRequestedSystemIdAsync(sector);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await EnsureSelectedColonySummaryVisibleAsync();
        await EnsureSelectedColonyDetailAsync();
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

        await ConfigureColonyObserverAsync();
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
        ClearColonyFilters();
        await LoadColonyPageAsync(resetList: true);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, selectedSystemId, colonyId: selectedColonyId));
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, selectedSystemId, colonyId: selectedColonyId));
        }

        return PersistExplorerSessionAsync();
    }

    protected Task HandleSearchQueryChangedAsync(string value)
    {
        searchQuery = value;
        return RunSearchAsync();
    }

    private async Task<int> ResolveRequestedSystemIdAsync(StarWinSector sector, CancellationToken cancellationToken = default)
    {
        if (RequestedSystemId is int requestedSystemId
            && sector.Systems.Any(system => system.Id == requestedSystemId))
        {
            return requestedSystemId;
        }

        if (RequestedColonyId is int requestedColonyId)
        {
            var resolvedSystemId = await ExplorerQueryService.ResolveSystemIdAsync(
                sector.Id,
                colonyId: requestedColonyId,
                cancellationToken: cancellationToken);
            if (resolvedSystemId is int requestedSelectionSystemId
                && sector.Systems.Any(system => system.Id == requestedSelectionSystemId))
            {
                return requestedSelectionSystemId;
            }
        }

        return ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
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

    protected void SelectColony(int colonyId)
    {
        var item = loadedColonySummaries.FirstOrDefault(summary => summary.ColonyId == colonyId);
        selectedColonyId = colonyId;
        if (item is not null && item.SystemId > 0)
        {
            selectedSystemId = item.SystemId;
            selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
        }

        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, selectedSystemId, colonyId: colonyId), replace: true);
    }

    protected void NavigateToWorld(int worldId)
    {
        var targetSystemId = selectedColonyDetail?.World.StarSystemId ?? selectedSystemId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, targetSystemId, worldId: worldId, colonyId: selectedColonyId));
    }

    protected void NavigateToRace(int raceId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId, colonyId: selectedColonyId));
    }

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await LoadColonyPageAsync(resetList: true);
    }

    protected Task HandleColonyFiltersChangedAsync()
    {
        ResetColonyWindow();
        return LoadColonyPageAsync(resetList: true);
    }

    protected Task ApplyColonyRaceFilterAsync()
    {
        colonyRaceId = ParseComboId(colonyRaceText);
        ResetColonyWindow();
        return LoadColonyPageAsync(resetList: true);
    }

    protected Task ApplyColonyEmpireFilterAsync()
    {
        colonyEmpireId = ParseComboId(colonyEmpireText);
        ResetColonyWindow();
        return LoadColonyPageAsync(resetList: true);
    }

    protected void ToggleColonyFilters()
    {
        showColonyFilters = !showColonyFilters;
    }

    protected Task HandleClearColonyFiltersAsync()
    {
        ClearColonyFilters();
        return LoadColonyPageAsync(resetList: true);
    }

    [JSInvokable]
    public Task LoadMoreColonies()
    {
        colonyObserverConfigured = false;
        return LoadMoreColoniesAsync();
    }

    protected IReadOnlyList<EntityImage> GetEntityImages(EntityImageTargetKind targetKind, int targetId)
    {
        return entityImages
            .Where(image => image.TargetKind == targetKind && image.TargetId == targetId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.UploadedAt)
            .ToList();
    }

    protected static string DisplayPopulation(long population)
    {
        return population >= 1_000_000_000
            ? $"{population / 1_000_000_000m:0.#} billion"
            : $"{population / 1_000_000m:0.#} million";
    }

    protected static string DisplayColonyName(Colony colony)
    {
        return string.IsNullOrWhiteSpace(colony.Name) ? colony.ColonyClass : colony.Name;
    }

    protected static long GetDemographicPopulation(Colony colony, ColonyDemographic demographic)
    {
        if (demographic.Population > 0)
        {
            return demographic.Population;
        }

        return demographic.PopulationPercent <= 0
            ? 0
            : (long)Math.Round(colony.EstimatedPopulation * demographic.PopulationPercent / 100m);
    }

    private async Task RefreshExplorerShellAsync(int? preferredSectorId = null, CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: preferredSectorId,
            cancellationToken: cancellationToken);
    }

    private async Task LoadColonyPageAsync(bool resetList, CancellationToken cancellationToken = default)
    {
        if (selectedSectorId <= 0)
        {
            loadedColonySummaries.Clear();
            colonyHasMoreRecords = false;
            ClearSelectedColonyDetail();
            colonyFilterOptions = new([], [], []);
            return;
        }

        if (resetList || loadedColonySectorId != selectedSectorId)
        {
            loadedColonySummaries.Clear();
            loadedColonySectorId = selectedSectorId;
            colonyHasMoreRecords = false;
            colonyObserverConfigured = false;
            ClearSelectedColonyDetail();
            colonyFilterOptions = await ExplorerQueryService.LoadColonyFilterOptionsAsync(selectedSectorId, cancellationToken);
            await LoadMoreColoniesAsync(cancellationToken);
        }

        await EnsureSelectedColonySummaryVisibleAsync(cancellationToken);
        await EnsureSelectedColonyDetailAsync(cancellationToken);
    }

    private async Task LoadMoreColoniesAsync(CancellationToken cancellationToken = default)
    {
        if (colonyListLoading || selectedSectorId <= 0)
        {
            return;
        }

        colonyListLoading = true;
        try
        {
            var page = await ExplorerQueryService.LoadColonyListPageAsync(
                new ExplorerColonyListPageRequest(
                    selectedSectorId,
                    loadedColonySummaries.Count,
                    ExplorerListBatchSize,
                    string.IsNullOrWhiteSpace(colonyQuery) ? null : colonyQuery.Trim(),
                    colonyRaceId == ComboAllFilterId ? null : colonyRaceId,
                    colonyEmpireId == ComboAllFilterId ? null : colonyEmpireId,
                    string.IsNullOrWhiteSpace(colonyStatus) ? null : colonyStatus.Trim(),
                    string.IsNullOrWhiteSpace(colonyClass) ? null : colonyClass.Trim()),
                cancellationToken);

            foreach (var item in page.Items)
            {
                if (loadedColonySummaries.All(existing => existing.ColonyId != item.ColonyId))
                {
                    loadedColonySummaries.Add(item);
                }
            }

            colonyHasMoreRecords = page.HasMore;
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            colonyListLoading = false;
        }
    }

    private async Task EnsureSelectedColonySummaryVisibleAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveColonyFilters())
        {
            return;
        }

        var targetColonyId = RequestedColonyId ?? selectedColonyId;
        if (targetColonyId <= 0 || loadedColonySummaries.Any(item => item.ColonyId == targetColonyId))
        {
            return;
        }

        var requestedItem = await ExplorerQueryService.LoadColonyListItemAsync(selectedSectorId, targetColonyId, cancellationToken);
        if (requestedItem is null)
        {
            return;
        }

        loadedColonySummaries.Add(requestedItem);
        loadedColonySummaries.Sort((left, right) =>
        {
            var populationComparison = right.EstimatedPopulation.CompareTo(left.EstimatedPopulation);
            if (populationComparison != 0)
            {
                return populationComparison;
            }

            var worldComparison = string.Compare(left.WorldName, right.WorldName, StringComparison.OrdinalIgnoreCase);
            return worldComparison != 0 ? worldComparison : left.ColonyId.CompareTo(right.ColonyId);
        });
    }

    private async Task EnsureSelectedColonyDetailAsync(CancellationToken cancellationToken = default)
    {
        var targetColonyId = HasActiveColonyFilters()
            ? selectedColonyId
            : RequestedColonyId ?? selectedColonyId;

        if (targetColonyId <= 0)
        {
            ClearSelectedColonyDetail();
            return;
        }

        if (selectedColonyDetail?.Colony.Id == targetColonyId && loadedColonyDetailId == targetColonyId)
        {
            selectedColonyId = targetColonyId;
            return;
        }

        if (colonyDetailLoading)
        {
            return;
        }

        colonyDetailLoading = true;
        try
        {
            var detail = await ExplorerQueryService.LoadColonyDetailAsync(selectedSectorId, targetColonyId, cancellationToken);
            selectedColonyDetail = detail;
            selectedColonyId = detail?.Colony.Id ?? 0;
            loadedColonyDetailId = selectedColonyId;
            if (detail?.World.StarSystemId is int systemId && systemId > 0)
            {
                selectedSystemId = systemId;
                selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
            }

            await EnsureEntityImagesLoadedAsync(cancellationToken);
        }
        finally
        {
            colonyDetailLoading = false;
        }
    }

    private void ClearSelectedColonyDetail()
    {
        selectedColonyId = 0;
        selectedColonyDetail = null;
        loadedColonyDetailId = 0;
        entityImages = [];
        lastLoadedImageKey = string.Empty;
    }

    private async Task EnsureEntityImagesLoadedAsync(CancellationToken cancellationToken = default)
    {
        var targets = selectedColonyDetail is null
            ? []
            : new List<EntityImageTarget> { new(EntityImageTargetKind.Colony, selectedColonyDetail.Colony.Id) };
        var imageKey = string.Join('|', targets.Select(target => $"{(int)target.TargetKind}:{target.TargetId}"));
        if (entityImagesLoading || imageKey == lastLoadedImageKey)
        {
            return;
        }

        entityImagesLoading = true;
        try
        {
            entityImages = await ImageService.GetImagesAsync(targets, cancellationToken);
            lastLoadedImageKey = imageKey;
            explorerRenderError = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            entityImagesLoading = false;
        }
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
        await LoadColonyPageAsync(resetList: true);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, selectedSystemId, colonyId: selectedColonyId), replace: true);
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
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Colonies")));
    }

    private async Task UploadEntityImage(EntityImageTargetKind targetKind, int targetId, InputFileChangeEventArgs args)
    {
        var file = args.File;
        try
        {
            await using var stream = file.OpenReadStream(10 * 1024 * 1024);
            await ImageService.UploadImageAsync(targetKind, targetId, file.Name, file.ContentType, stream);
            lastLoadedImageKey = string.Empty;
            await EnsureEntityImagesLoadedAsync();
            imageUploadStatus = $"{file.Name} uploaded.";
        }
        catch (Exception exception)
        {
            imageUploadStatus = exception.Message;
        }
    }

    private async Task ConfigureColonyObserverAsync()
    {
        if (!colonyHasMoreRecords)
        {
            colonyObserverConfigured = false;
            if (loadMoreScrollModule is not null)
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectColonyLoadMore");
            }

            return;
        }

        if (colonyObserverConfigured)
        {
            return;
        }

        loadMoreScrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/timelineInfiniteScroll.js");
        dotNetReference ??= DotNetObjectReference.Create(this);
        await loadMoreScrollModule.InvokeVoidAsync("observeColonyLoadMore", colonyLoadMoreElement, dotNetReference);
        colonyObserverConfigured = true;
    }

    private void ClearColonyFilters()
    {
        colonyQuery = string.Empty;
        colonyRaceText = string.Empty;
        colonyEmpireText = string.Empty;
        colonyRaceId = ComboAllFilterId;
        colonyEmpireId = ComboAllFilterId;
        colonyStatus = string.Empty;
        colonyClass = string.Empty;
        showColonyFilters = false;
        ResetColonyWindow();
    }

    private void ResetColonyWindow()
    {
        colonyObserverConfigured = false;
    }

    private bool HasActiveColonyFilters()
    {
        return !string.IsNullOrWhiteSpace(colonyQuery)
            || colonyRaceId != ComboAllFilterId
            || colonyEmpireId != ComboAllFilterId
            || !string.IsNullOrWhiteSpace(colonyStatus)
            || !string.IsNullOrWhiteSpace(colonyClass);
    }

    private static string FormatSelectedSystem(StarWinSector sector, int systemId)
    {
        var system = sector.Systems.FirstOrDefault(item => item.Id == systemId);
        return system is null ? string.Empty : $"{system.Id} - {system.Name}";
    }

    private static string FormatLookupOption(ExplorerLookupOption option) => $"{option.Id} - {option.Name}";

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

    public async ValueTask DisposeAsync()
    {
        if (loadMoreScrollModule is not null)
        {
            try
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectColonyLoadMore");
                await loadMoreScrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }
}
