using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Globalization;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
using StarWin.Domain.Services;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Aliens : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 30;
    private const int ComboAllFilterId = -1;
    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected IStarWinSearchService SearchService { get; set; } = default!;
    [Inject] protected IStarWinImageService ImageService { get; set; } = default!;
    [Inject] protected IStarWinEntityNameService EntityNameService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

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
    protected bool raceHasMoreRecords;
    protected bool showRaceFilters;
    protected ExplorerAlienRaceDetail? selectedRaceDetail;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoaded;
    private bool entityImagesLoading;
    private bool raceListLoading;
    private bool raceDetailLoading;
    private bool raceSearchInProgress;
    private string imageUploadStatus = string.Empty;
    private ElementReference raceLoadMoreElement;
    private DotNetObjectReference<Aliens>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool raceObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int raceSearchVersion;
    private int loadedRaceSectorId;
    private readonly List<ExplorerAlienRaceListItem> loadedRaceSummaries = [];

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerAlienRaceListItem> LoadedRaceSummaries => loadedRaceSummaries;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadRacePageAsync(resetList: true);
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
            await LoadRacePageAsync(resetList: true);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await EnsureSelectedRaceDetailAsync();
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
        RunSearch();
        return Task.CompletedTask;
    }

    protected void NavigateToSearchResult(StarWinSearchResult result)
    {
        var targetUri = SectorExplorerRoutes.BuildSectionUri(
            result.Tab,
            result.SectorId ?? selectedSectorId,
            result.SystemId ?? 0,
            result.WorldId ?? 0,
            result.Type == StarWinSearchResultType.Colony ? result.WorldId ?? 0 : 0,
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

    protected IEnumerable<AlienRace> GetFilteredRaces(IReadOnlyList<AlienRace> races)
    {
        return races
            .Where(RaceMatches)
            .OrderBy(race => race.Name)
            .ThenBy(race => race.Id);
    }

    protected IEnumerable<ExplorerAlienRaceListItem> GetFilteredRaces(IReadOnlyList<ExplorerAlienRaceListItem> races)
    {
        return races
            .Where(RaceMatches)
            .OrderBy(race => race.Name)
            .ThenBy(race => race.RaceId);
    }

    protected void ResetRaceWindow()
    {
        raceObserverConfigured = false;
    }

    protected Task ResetRaceWindowAsync()
    {
        ResetRaceWindow();
        raceSearchVersion++;
        raceSearchInProgress = false;

        if (HasActiveRaceFilters())
        {
            _ = SearchRemainingRacePagesAsync(raceSearchVersion);
        }

        return Task.CompletedTask;
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
        showRaceFilters = false;
        raceSearchVersion++;
        raceSearchInProgress = false;
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

    private bool RaceMatches(AlienRace race)
    {
        if (!string.IsNullOrWhiteSpace(raceEnvironment)
            && !string.Equals(race.EnvironmentType, raceEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(raceAppearance)
            && !string.Equals(race.AppearanceType, raceAppearance, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(raceQuery))
        {
            return true;
        }

        var query = raceQuery.Trim();
        return ContainsQuery(race.Name, query)
            || ContainsQuery(race.Id.ToString(), query)
            || ContainsQuery(race.AppearanceType, query)
            || ContainsQuery(race.BodyChemistry, query)
            || ContainsQuery(race.EnvironmentType, query)
            || ContainsQuery(race.ReproductionMethod, query)
            || ContainsQuery(race.Diet, query)
            || ContainsQuery(race.BiologyProfile.Body.ToString(), query)
            || ContainsQuery(race.BiologyProfile.Mind.ToString(), query)
            || ContainsQuery(race.BiologyProfile.Speed.ToString(), query)
            || ContainsQuery(race.BiologyProfile.PsiRating.ToString(), query)
            || ContainsQuery(selectedRaceDetail?.HomeWorld?.Name, query);
    }

    private bool RaceMatches(ExplorerAlienRaceListItem race)
    {
        if (!string.IsNullOrWhiteSpace(raceEnvironment)
            && !string.Equals(race.EnvironmentType, raceEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(raceAppearance)
            && !string.Equals(race.AppearanceType, raceAppearance, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(raceQuery))
        {
            return true;
        }

        var query = raceQuery.Trim();
        return ContainsQuery(race.Name, query)
            || ContainsQuery(race.RaceId.ToString(CultureInfo.InvariantCulture), query)
            || ContainsQuery(race.AppearanceType, query)
            || ContainsQuery(race.EnvironmentType, query);
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
        if (entityImagesLoaded || entityImagesLoading)
        {
            return;
        }

        entityImagesLoading = true;
        try
        {
            entityImages = await ImageService.GetImagesAsync();
            entityImagesLoaded = true;
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
            includeSavedRoutes: false,
            includeReferenceData: false,
            cancellationToken: cancellationToken);
    }

    private void RunSearch()
    {
        searchResults = SearchService.Search(searchQuery)
            .Where(result => result.SectorId is null || result.SectorId == selectedSectorId)
            .ToList();
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
        raceSearchVersion++;
        raceSearchInProgress = false;

        if (selectedSectorId <= 0)
        {
            loadedRaceSummaries.Clear();
            raceHasMoreRecords = false;
            selectedRaceId = 0;
            selectedRaceDetail = null;
            return;
        }

        if (resetList || loadedRaceSectorId != selectedSectorId)
        {
            loadedRaceSummaries.Clear();
            loadedRaceSectorId = selectedSectorId;
            raceHasMoreRecords = false;
            raceObserverConfigured = false;
            await LoadMoreRaceSummariesAsync(cancellationToken);
        }

        await EnsureSelectedRaceSummaryVisibleAsync(cancellationToken);
        await EnsureSelectedRaceDetailAsync(cancellationToken);
    }

    private async Task LoadMoreRaceSummariesAsync(CancellationToken cancellationToken = default)
    {
        if (raceListLoading || selectedSectorId <= 0)
        {
            return;
        }

        raceListLoading = true;
        try
        {
            var page = await ExplorerQueryService.LoadAlienRaceListPageAsync(
                new ExplorerAlienRaceListPageRequest(selectedSectorId, loadedRaceSummaries.Count, ExplorerListBatchSize),
                cancellationToken);

            foreach (var item in page.Items)
            {
                if (loadedRaceSummaries.All(existing => existing.RaceId != item.RaceId))
                {
                    loadedRaceSummaries.Add(item);
                }
            }

            raceHasMoreRecords = page.HasMore;
            if (selectedRaceId == 0 && loadedRaceSummaries.Count > 0)
            {
                selectedRaceId = loadedRaceSummaries[0].RaceId;
            }

            await EnsureSelectedRaceSummaryVisibleAsync(cancellationToken);
            await EnsureSelectedRaceDetailAsync(cancellationToken);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            raceListLoading = false;
        }
    }

    private async Task SearchRemainingRacePagesAsync(int searchVersion, CancellationToken cancellationToken = default)
    {
        if (selectedSectorId <= 0 || !HasActiveRaceFilters())
        {
            return;
        }

        if (GetFilteredRaces(LoadedRaceSummaries).Any() || !raceHasMoreRecords)
        {
            return;
        }

        raceSearchInProgress = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            while (searchVersion == raceSearchVersion
                && HasActiveRaceFilters()
                && raceHasMoreRecords
                && !GetFilteredRaces(LoadedRaceSummaries).Any())
            {
                await LoadMoreRaceSummariesAsync(cancellationToken);
            }
        }
        finally
        {
            if (searchVersion == raceSearchVersion)
            {
                raceSearchInProgress = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task EnsureSelectedRaceSummaryVisibleAsync(CancellationToken cancellationToken = default)
    {
        var requestedRaceId = RequestedRaceId ?? selectedRaceId;
        if (requestedRaceId <= 0 || loadedRaceSummaries.Any(item => item.RaceId == requestedRaceId))
        {
            return;
        }

        var requestedItem = await ExplorerQueryService.LoadAlienRaceListItemAsync(selectedSectorId, requestedRaceId, cancellationToken);
        if (requestedItem is null)
        {
            return;
        }

        loadedRaceSummaries.Add(requestedItem);
        loadedRaceSummaries.Sort((left, right) =>
        {
            var nameComparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0 ? nameComparison : left.RaceId.CompareTo(right.RaceId);
        });
    }

    private async Task EnsureSelectedRaceDetailAsync(CancellationToken cancellationToken = default)
    {
        var targetRaceId = RequestedRaceId ?? selectedRaceId;
        if (targetRaceId <= 0)
        {
            targetRaceId = loadedRaceSummaries.FirstOrDefault()?.RaceId ?? 0;
        }

        if (targetRaceId <= 0)
        {
            selectedRaceId = 0;
            selectedRaceDetail = null;
            return;
        }

        if (raceDetailLoading)
        {
            return;
        }

        raceDetailLoading = true;
        try
        {
            var detail = await ExplorerQueryService.LoadAlienRaceDetailAsync(selectedSectorId, targetRaceId, cancellationToken);
            if (detail is null && loadedRaceSummaries.Count > 0)
            {
                targetRaceId = loadedRaceSummaries[0].RaceId;
                detail = await ExplorerQueryService.LoadAlienRaceDetailAsync(selectedSectorId, targetRaceId, cancellationToken);
            }

            selectedRaceDetail = detail;
            selectedRaceId = detail?.Race.Id ?? 0;
        }
        finally
        {
            raceDetailLoading = false;
        }
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
            entityImages = await ImageService.GetImagesAsync();
            entityImagesLoaded = true;
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

    private static bool ContainsQuery(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool HasActiveRaceFilters()
    {
        return !string.IsNullOrWhiteSpace(raceQuery)
            || !string.IsNullOrWhiteSpace(raceEnvironment)
            || !string.IsNullOrWhiteSpace(raceAppearance);
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
