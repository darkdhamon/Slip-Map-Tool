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
    private const int ColonyBatchSize = 120;
    private const int ComboAllFilterId = -1;
    private const ExplorerSectorLoadSections DetailedSectorSections = ExplorerSectorLoadSections.Worlds
        | ExplorerSectorLoadSections.Colonies
        | ExplorerSectorLoadSections.ColonyDemographics;
    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinSearchService SearchService { get; set; } = default!;
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
    protected readonly ExplorerSectorCacheBuilder sectorCacheBuilder = new();
    protected readonly Dictionary<int, World> WorldsById = [];

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
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
    protected int colonyVisibleCount = ColonyBatchSize;
    protected bool colonyHasMoreRecords;
    protected bool showColonyFilters;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoaded;
    private bool entityImagesLoading;
    protected string imageUploadStatus = string.Empty;
    private ElementReference colonyLoadMoreElement;
    private DotNetObjectReference<Colonies>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool colonyObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<AlienRace> ExplorerAlienRaces => explorerContext.AlienRaces;
    protected IReadOnlyList<Empire> ExplorerEmpires => explorerContext.Empires;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync(RequestedSectorId);
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        selectedColonyId = ResolveSelectedColonyId(initialSector);
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
            await RefreshExplorerDataAsync(selectedSectorId);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        selectedColonyId = ResolveSelectedColonyId(sector);
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
        _ = EnsureEntityImagesLoadedAsync(backgroundLoad: true);
    }

    protected ExplorerSectorCache GetSectorCache(StarWinSector sector) => sectorCacheBuilder.Get(sector);

    protected ExplorerSectorSummary GetSectorSummary(StarWinSector sector) => sectorCacheBuilder.GetSummary(sector);

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
        await RefreshExplorerDataAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        selectedColonyId = ResolveSelectedColonyId(sector);
        ClearColonyFilters();
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

    protected void SelectColony(int colonyId)
    {
        selectedColonyId = colonyId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, selectedSystemId, colonyId: colonyId), replace: true);
    }

    protected void NavigateToWorld(int worldId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, selectedSystemId, worldId: worldId, colonyId: selectedColonyId));
    }

    protected void NavigateToRace(int raceId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId, colonyId: selectedColonyId));
    }

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await RefreshExplorerDataAsync(selectedSectorId);
        selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
    }

    protected IEnumerable<(World World, Colony Colony)> GetFilteredColonies(ExplorerSectorCache sectorCache)
    {
        return sectorCache.ColoniesByPopulation.Where(ColonyMatches);
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

    [JSInvokable]
    public async Task LoadMoreColonies()
    {
        if (loadMoreScrollModule is not null)
        {
            await loadMoreScrollModule.InvokeVoidAsync("disconnectColonyLoadMore");
        }

        colonyVisibleCount += ColonyBatchSize;
        colonyObserverConfigured = false;
        StateHasChanged();
    }

    protected void ResetColonyWindow()
    {
        colonyVisibleCount = ColonyBatchSize;
        colonyObserverConfigured = false;
    }

    protected void ClearColonyFilters()
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

    protected void ToggleColonyFilters()
    {
        showColonyFilters = !showColonyFilters;
    }

    protected void ApplyColonyRaceFilter()
    {
        colonyRaceId = ParseComboId(colonyRaceText);
        ResetColonyWindow();
    }

    protected void ApplyColonyEmpireFilter()
    {
        colonyEmpireId = ParseComboId(colonyEmpireText);
        ResetColonyWindow();
    }

    private bool ColonyMatches((World World, Colony Colony) item)
    {
        if (colonyRaceId != ComboAllFilterId
            && item.Colony.RaceId != colonyRaceId
            && !item.Colony.Demographics.Any(demographic => demographic.RaceId == colonyRaceId))
        {
            return false;
        }

        if (colonyEmpireId != ComboAllFilterId
            && item.Colony.AllegianceId != colonyEmpireId
            && item.Colony.ControllingEmpireId != colonyEmpireId
            && item.Colony.FoundingEmpireId != colonyEmpireId
            && item.Colony.ParentEmpireId != colonyEmpireId)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(colonyStatus)
            && !string.Equals(item.Colony.PoliticalStatus.ToString(), colonyStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(colonyClass)
            && !string.Equals(item.Colony.ColonyClass, colonyClass, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(colonyQuery))
        {
            return true;
        }

        var query = colonyQuery.Trim();
        return ContainsQuery(item.World.Name, query)
            || ContainsQuery(item.World.WorldType, query)
            || ContainsQuery(item.Colony.Id.ToString(), query)
            || ContainsQuery(item.Colony.Name, query)
            || ContainsQuery(item.Colony.WorldId.ToString(), query)
            || ContainsQuery(item.Colony.ColonyClass, query)
            || ContainsQuery(item.Colony.ColonistRaceName, query)
            || ContainsQuery(item.Colony.AllegianceName, query)
            || ContainsQuery(item.Colony.Starport, query)
            || ContainsQuery(item.Colony.GovernmentType, query)
            || ContainsQuery(item.Colony.ExportResource, query)
            || ContainsQuery(item.Colony.ImportResource, query)
            || item.Colony.Demographics.Any(demographic => ContainsQuery(demographic.RaceName, query));
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

    private async Task RefreshExplorerDataAsync(int? detailedSectorId = null, CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            includeSavedRoutes: false,
            includeReferenceData: true,
            detailedSectorId: detailedSectorId,
            detailedSectorSections: DetailedSectorSections,
            cancellationToken: cancellationToken);

        foreach (var sectorId in ExplorerSectors.Select(sector => sector.Id))
        {
            sectorCacheBuilder.Invalidate(sectorId);
        }
        WorldsById.Clear();
        foreach (var world in explorerContext.Sectors.SelectMany(sector => sector.Systems).SelectMany(system => system.Worlds))
        {
            WorldsById[world.Id] = world;
        }
    }

    private int ResolveSelectedColonyId(StarWinSector sector)
    {
        if (RequestedColonyId is int requestedColonyId
            && sector.Systems.SelectMany(system => system.Worlds).Any(world => world.Colony?.Id == requestedColonyId))
        {
            return requestedColonyId;
        }

        if (selectedColonyId > 0
            && sector.Systems.SelectMany(system => system.Worlds).Any(world => world.Colony?.Id == selectedColonyId))
        {
            return selectedColonyId;
        }

        return sector.Systems.FirstOrDefault(system => system.Id == selectedSystemId)?.Worlds.FirstOrDefault()?.Colony?.Id
            ?? sector.Systems.SelectMany(system => system.Worlds).FirstOrDefault(world => world.Colony is not null)?.Colony?.Id
            ?? 0;
    }

    private void RunSearch()
    {
        var sector = GetSelectedSector();
        var sectorSummary = GetSectorSummary(sector);

        searchResults = SearchService.Search(searchQuery)
            .Where(result => result.Type switch
            {
                StarWinSearchResultType.AlienRace => result.RaceId is int raceId && sectorSummary.RaceIds.Contains(raceId),
                StarWinSearchResultType.Empire => result.EmpireId is int empireId && sectorSummary.EmpireIds.Contains(empireId),
                _ => result.SectorId is null || result.SectorId == sector.Id
            })
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
        await RefreshExplorerDataAsync(selectedSectorId);
        sector = GetSelectedSector();
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        selectedColonyId = ResolveSelectedColonyId(sector);
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

    private static string FormatRaceOption(AlienRace race) => $"{race.Id} - {race.Name}";

    private static string FormatEmpireOption(Empire empire) => $"{empire.Id} - {empire.Name}";

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
