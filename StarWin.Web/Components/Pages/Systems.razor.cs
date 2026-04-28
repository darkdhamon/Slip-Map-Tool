using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Systems : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 120;
    private const int ComboAllFilterId = -1;

    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected IStarWinImageService ImageService { get; set; } = default!;
    [Inject] protected IStarWinEntityNameService EntityNameService { get; set; } = default!;
    [Inject] protected IStarWinSpaceHabitatService SpaceHabitatService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected ExplorerSystemFilterOptions systemFilterOptions = new([]);
    protected ExplorerSystemDetail? selectedSystemDetail;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string systemQuery = string.Empty;
    protected string systemEmpireText = string.Empty;
    protected int spaceHabitatEmpireId;
    protected string spaceHabitatName = string.Empty;
    protected string spaceHabitatStatus = string.Empty;
    protected string imageUploadStatus = string.Empty;
    protected bool systemHasMoreRecords;
    protected bool showSystemFilters;
    protected bool IsSpaceHabitatCreateDisabled => spaceHabitatEmpireId == 0;

    private readonly List<ExplorerSystemListItem> loadedSystemSummaries = [];
    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoading;
    private bool systemListLoading;
    private bool systemDetailLoading;
    private bool systemObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int systemEmpireId = ComboAllFilterId;
    private int loadedSystemSectorId;
    private string lastLoadedImageKey = string.Empty;
    private ElementReference systemLoadMoreElement;
    private DotNetObjectReference<Systems>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerSystemListItem> LoadedSystemSummaries => loadedSystemSummaries;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerShellAsync(RequestedSectorId);
        var initialSector = GetInitialSector();
        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadSystemPageAsync(resetList: true);
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
            selectedSystemId = 0;
            await LoadSystemPageAsync(resetList: true);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await EnsureSelectedSystemDetailAsync();
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

        await ConfigureSystemObserverAsync();
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
        selectedSystemId = 0;
        selectedSystemText = string.Empty;
        ClearSystemFilters();
        await PersistExplorerSessionAsync();
        await LoadSystemPageAsync(resetList: true);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, selectedSystemId));
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, systemId), replace: true);
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

    protected void SelectSystem(int systemId)
    {
        selectedSystemId = systemId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, systemId), replace: true);
    }

    protected void NavigateToWorld(int worldId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, selectedSystemId, worldId: worldId));
    }

    protected void NavigateToHabitat(int habitatId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, selectedSystemId, habitatId: habitatId));
    }

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await RefreshExplorerShellAsync(selectedSectorId);
        await LoadSystemPageAsync(resetList: true);
    }

    protected Task HandleSystemFiltersChangedAsync()
    {
        ResetSystemWindow();
        return LoadSystemPageAsync(resetList: true);
    }

    protected Task ApplySystemEmpireFilterAsync()
    {
        systemEmpireId = ParseComboId(systemEmpireText);
        ResetSystemWindow();
        return LoadSystemPageAsync(resetList: true);
    }

    protected void ToggleSystemFilters()
    {
        showSystemFilters = !showSystemFilters;
    }

    protected Task HandleClearSystemFiltersAsync()
    {
        ClearSystemFilters();
        return LoadSystemPageAsync(resetList: true);
    }

    [JSInvokable]
    public Task LoadMoreSystems()
    {
        systemObserverConfigured = false;
        return LoadMoreSystemsAsync();
    }

    protected IReadOnlyList<EntityImage> GetEntityImages(EntityImageTargetKind targetKind, int targetId)
    {
        return entityImages
            .Where(image => image.TargetKind == targetKind && image.TargetId == targetId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.UploadedAt)
            .ToList();
    }

    protected EntityImage? GetPrimaryEntityImage(EntityImageTargetKind targetKind, int targetId)
    {
        return GetEntityImages(targetKind, targetId).FirstOrDefault();
    }

    protected string GetAstralBodyOrbClass(AstralBody body)
    {
        return $"stellar-orb {body.Kind.ToString().ToLowerInvariant()}";
    }

    protected string DisplayAllegiance(ushort allegianceId)
    {
        if (allegianceId == ushort.MaxValue)
        {
            return "Independent";
        }

        return systemFilterOptions.Empires.FirstOrDefault(empire => empire.Id == allegianceId)?.Name ?? allegianceId.ToString();
    }

    protected static string DisplayCoordinates(Coordinates coordinates)
    {
        return $"{coordinates.XParsecs:0.#}, {coordinates.YParsecs:0.#}, {coordinates.ZParsecs:0.#}";
    }

    protected static string Format(double? value)
    {
        return value?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
    }

    private async Task CreateAstralBodySpaceHabitatAsync(StarSystem system, int astralBodySequence)
    {
        if (spaceHabitatEmpireId == 0)
        {
            spaceHabitatStatus = "Choose an empire.";
            return;
        }

        try
        {
            var habitat = await SpaceHabitatService.CreateOrbitingAstralBodyAsync(
                system.Id,
                astralBodySequence,
                spaceHabitatEmpireId,
                spaceHabitatName);

            await LoadSystemDetailAsync(system.Id);
            spaceHabitatName = string.Empty;
            spaceHabitatStatus = $"{habitat.Name} created.";
            NavigateToHabitat(habitat.Id);
        }
        catch (InvalidOperationException ex)
        {
            spaceHabitatStatus = ex.Message;
        }
    }

    private async Task RefreshExplorerShellAsync(int? preferredSectorId = null, CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: preferredSectorId,
            cancellationToken: cancellationToken);
    }

    private async Task LoadSystemPageAsync(bool resetList, CancellationToken cancellationToken = default)
    {
        if (selectedSectorId <= 0)
        {
            loadedSystemSummaries.Clear();
            systemHasMoreRecords = false;
            selectedSystemDetail = null;
            selectedSystemId = 0;
            entityImages = [];
            systemFilterOptions = new([]);
            return;
        }

        if (resetList || loadedSystemSectorId != selectedSectorId)
        {
            loadedSystemSummaries.Clear();
            loadedSystemSectorId = selectedSectorId;
            systemHasMoreRecords = false;
            systemObserverConfigured = false;
            systemFilterOptions = await ExplorerQueryService.LoadSystemFilterOptionsAsync(selectedSectorId, cancellationToken);
            await LoadMoreSystemsAsync(cancellationToken);
        }

        await EnsureSelectedSystemSummaryVisibleAsync(cancellationToken);
        await EnsureSelectedSystemDetailAsync(cancellationToken);
    }

    private async Task LoadMoreSystemsAsync(CancellationToken cancellationToken = default)
    {
        if (systemListLoading || selectedSectorId <= 0)
        {
            return;
        }

        systemListLoading = true;
        try
        {
            var page = await ExplorerQueryService.LoadSystemListPageAsync(
                new ExplorerSystemListPageRequest(
                    selectedSectorId,
                    loadedSystemSummaries.Count,
                    ExplorerListBatchSize,
                    string.IsNullOrWhiteSpace(systemQuery) ? null : systemQuery.Trim(),
                    systemEmpireId == ComboAllFilterId ? null : systemEmpireId),
                cancellationToken);

            foreach (var item in page.Items)
            {
                if (loadedSystemSummaries.All(existing => existing.SystemId != item.SystemId))
                {
                    loadedSystemSummaries.Add(item);
                }
            }

            systemHasMoreRecords = page.HasMore;
            if (selectedSystemId == 0 && loadedSystemSummaries.Count > 0)
            {
                selectedSystemId = loadedSystemSummaries[0].SystemId;
            }
            else if (selectedSystemId > 0 && loadedSystemSummaries.All(item => item.SystemId != selectedSystemId))
            {
                selectedSystemId = loadedSystemSummaries.FirstOrDefault()?.SystemId ?? 0;
            }

            await EnsureSelectedSystemSummaryVisibleAsync(cancellationToken);
            await EnsureSelectedSystemDetailAsync(cancellationToken);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            systemListLoading = false;
        }
    }

    private async Task EnsureSelectedSystemSummaryVisibleAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveSystemFilters())
        {
            return;
        }

        var targetSystemId = RequestedSystemId ?? selectedSystemId;
        if (targetSystemId <= 0 || loadedSystemSummaries.Any(item => item.SystemId == targetSystemId))
        {
            return;
        }

        var requestedItem = await ExplorerQueryService.LoadSystemListItemAsync(selectedSectorId, targetSystemId, cancellationToken);
        if (requestedItem is null)
        {
            return;
        }

        loadedSystemSummaries.Add(requestedItem);
        loadedSystemSummaries.Sort((left, right) =>
        {
            var nameComparison = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0 ? nameComparison : left.SystemId.CompareTo(right.SystemId);
        });
    }

    private async Task EnsureSelectedSystemDetailAsync(CancellationToken cancellationToken = default)
    {
        var targetSystemId = HasActiveSystemFilters()
            ? selectedSystemId
            : RequestedSystemId ?? selectedSystemId;
        if (targetSystemId <= 0)
        {
            targetSystemId = loadedSystemSummaries.FirstOrDefault()?.SystemId ?? 0;
        }

        if (targetSystemId <= 0 || systemDetailLoading)
        {
            if (targetSystemId <= 0)
            {
                selectedSystemId = 0;
                selectedSystemDetail = null;
            }

            return;
        }

        systemDetailLoading = true;
        try
        {
            var detail = await ExplorerQueryService.LoadSystemDetailAsync(selectedSectorId, targetSystemId, cancellationToken);
            if (detail is null && loadedSystemSummaries.Count > 0)
            {
                targetSystemId = loadedSystemSummaries[0].SystemId;
                detail = await ExplorerQueryService.LoadSystemDetailAsync(selectedSectorId, targetSystemId, cancellationToken);
            }

            selectedSystemDetail = detail;
            selectedSystemId = detail?.System.Id ?? 0;
            selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
            await EnsureEntityImagesLoadedAsync(cancellationToken);
        }
        finally
        {
            systemDetailLoading = false;
        }
    }

    private async Task LoadSystemDetailAsync(int systemId, CancellationToken cancellationToken = default)
    {
        if (systemId <= 0)
        {
            selectedSystemDetail = null;
            entityImages = [];
            return;
        }

        selectedSystemDetail = await ExplorerQueryService.LoadSystemDetailAsync(selectedSectorId, systemId, cancellationToken);
        await EnsureEntityImagesLoadedAsync(cancellationToken);
    }

    private async Task EnsureEntityImagesLoadedAsync(CancellationToken cancellationToken = default)
    {
        var targets = selectedSystemDetail?.System.AstralBodies
            .Select(body => new EntityImageTarget(EntityImageTargetKind.AstralBody, body.Id))
            .Distinct()
            .ToList()
            ?? [];

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
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await LoadSystemPageAsync(resetList: true);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, selectedSystemId), replace: true);
    }

    private async Task PersistExplorerSessionAsync()
    {
        await ExplorerPageState.PersistSelectionAsync(
            JS,
            browserSessionReady,
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Systems")));
    }

    private async Task ConfigureSystemObserverAsync()
    {
        if (!systemHasMoreRecords)
        {
            systemObserverConfigured = false;
            if (loadMoreScrollModule is not null)
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "system");
            }

            return;
        }

        if (systemObserverConfigured)
        {
            return;
        }

        loadMoreScrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/timelineInfiniteScroll.js");
        dotNetReference ??= DotNetObjectReference.Create(this);
        await loadMoreScrollModule.InvokeVoidAsync("observeLoadMore", "system", systemLoadMoreElement, dotNetReference, "LoadMoreSystems");
        systemObserverConfigured = true;
    }

    private void ClearSystemFilters()
    {
        systemQuery = string.Empty;
        systemEmpireText = string.Empty;
        systemEmpireId = ComboAllFilterId;
        showSystemFilters = false;
        ResetSystemWindow();
    }

    private void ResetSystemWindow()
    {
        systemObserverConfigured = false;
    }

    private bool HasActiveSystemFilters()
    {
        return !string.IsNullOrWhiteSpace(systemQuery)
            || systemEmpireId != ComboAllFilterId;
    }

    private static string FormatEmpireOption(ExplorerLookupOption empire)
    {
        return $"{empire.Id} - {empire.Name}";
    }

    private StarWinSector GetInitialSector()
    {
        return RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;
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

    private static IEnumerable<OrbitalSatellite> GetAstralBodySatellites(StarSystem system, int astralBodySequence)
    {
        foreach (var world in system.Worlds
            .Where(world => world.ParentWorldId is null && (world.PrimaryAstralBodySequence ?? 0) == astralBodySequence)
            .OrderBy(world => world.OrbitRadiusAu ?? double.MaxValue)
            .ThenBy(world => world.Name))
        {
            yield return OrbitalSatellite.FromWorld(world);
        }

        foreach (var habitat in system.SpaceHabitats
            .Where(habitat => habitat.OrbitTargetKind == OrbitTargetKind.AstralBody && habitat.OrbitTargetId == astralBodySequence)
            .OrderBy(habitat => habitat.OrbitRadiusKm ?? double.MaxValue)
            .ThenBy(habitat => habitat.Name))
        {
            yield return OrbitalSatellite.FromHabitat(habitat);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (loadMoreScrollModule is not null)
        {
            try
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "system");
                await loadMoreScrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }

    private sealed record OrbitalSatellite(string Name, string Kind, World? World, SpaceHabitat? Habitat)
    {
        public static OrbitalSatellite FromWorld(World world)
        {
            return new OrbitalSatellite(world.Name, world.Kind.ToString(), world, null);
        }

        public static OrbitalSatellite FromHabitat(SpaceHabitat habitat)
        {
            return new OrbitalSatellite(habitat.Name, "Habitat", null, habitat);
        }
    }
}
