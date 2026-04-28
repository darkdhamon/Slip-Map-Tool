using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Worlds : ComponentBase, IAsyncDisposable
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

    [SupplyParameterFromQuery(Name = "worldId")]
    public int? RequestedWorldId { get; set; }

    [SupplyParameterFromQuery(Name = "colonyId")]
    public int? RequestedColonyId { get; set; }

    [SupplyParameterFromQuery(Name = "habitatId")]
    public int? RequestedHabitatId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected ExplorerWorldsWorkspace? selectedWorldsWorkspace;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected int selectedWorldId;
    protected int selectedHabitatId;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string worldQuery = string.Empty;
    protected string worldKind = string.Empty;
    protected string worldType = string.Empty;
    protected bool worldHasMoreRecords;
    protected bool showWorldFilters;
    protected string imageUploadStatus = string.Empty;
    protected string spaceHabitatName = string.Empty;
    protected int spaceHabitatEmpireId;
    protected string spaceHabitatStatus = string.Empty;
    protected bool IsSpaceHabitatCreateDisabled => spaceHabitatEmpireId == 0;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoading;
    private ElementReference worldLoadMoreElement;
    private DotNetObjectReference<Worlds>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool worldObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;
    private int worldVisibleCount = ExplorerListBatchSize;
    private string lastLoadedImageKey = string.Empty;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerLookupOption> ExplorerEmpires => selectedWorldsWorkspace?.Empires ?? [];

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerShellAsync(RequestedSectorId);
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = await ResolveRequestedSystemIdAsync(initialSector);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadSelectedWorkspaceAsync();
        await ApplyRequestedSelectionAsync();
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
        }

        var sector = GetSelectedSector();
        selectedSystemId = await ResolveRequestedSystemIdAsync(sector);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        await LoadSelectedWorkspaceAsync();
        await ApplyRequestedSelectionAsync();
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

        await ConfigureWorldObserverAsync();
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
        ClearWorldFilters();
        await LoadSelectedWorkspaceAsync();
        SelectDefaultRecord(GetSelectedSystemRecord());
        await PersistExplorerSessionAsync();
        NavigateToCurrentSelection();
    }

    protected async Task HandleSelectedSystemTextChangedAsync(string value)
    {
        selectedSystemText = value;
        var sector = GetSelectedSector();
        var systemId = ParseComboId(selectedSystemText);
        if (systemId != ComboAllFilterId && sector.Systems.Any(system => system.Id == systemId))
        {
            selectedSystemId = systemId;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            await LoadSelectedWorkspaceAsync();
            SelectDefaultRecord(GetSelectedSystemRecord());
            NavigateToCurrentSelection(replace: true);
        }

        await PersistExplorerSessionAsync();
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

    protected void SelectWorld(int worldId)
    {
        selectedWorldId = worldId;
        selectedHabitatId = 0;
        _ = EnsureEntityImagesLoadedAsync();
        NavigateToCurrentSelection(replace: true);
        _ = PersistExplorerSessionAsync();
    }

    protected void SelectHabitat(int habitatId)
    {
        selectedHabitatId = habitatId;
        selectedWorldId = 0;
        _ = EnsureEntityImagesLoadedAsync();
        NavigateToCurrentSelection(replace: true);
        _ = PersistExplorerSessionAsync();
    }

    protected void SelectParentOfHabitat(SpaceHabitat habitat)
    {
        var system = GetSelectedSystemRecord();
        if (habitat.OrbitTargetKind == OrbitTargetKind.World
            && system?.Worlds.FirstOrDefault(world => world.Id == habitat.OrbitTargetId) is World parentWorld)
        {
            SelectWorld(parentWorld.Id);
            return;
        }

        OpenSelectedSystemRecord();
    }

    protected void OpenSelectedSystemRecord()
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, selectedSystemId));
    }

    protected void NavigateToColony(int colonyId)
    {
        NavigationManager.NavigateTo(
            SectorExplorerRoutes.BuildSectionUri(
                "Colonies",
                selectedSectorId,
                selectedSystemId,
                selectedWorldId,
                colonyId));
    }

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await LoadSelectedWorkspaceAsync();
        await ApplyRequestedSelectionAsync();
    }

    protected IEnumerable<ExplorerWorldRecord> GetFilteredExplorerWorldRecords(StarSystem? system)
    {
        if (system is null)
        {
            return [];
        }

        return GetExplorerWorldRecords(system)
            .Where(ExplorerWorldRecordMatches)
            .OrderBy(record => record.KindSort)
            .ThenBy(record => record.OrbitSort)
            .ThenBy(record => record.Name);
    }

    protected void ResetWorldWindow()
    {
        worldVisibleCount = ExplorerListBatchSize;
        worldObserverConfigured = false;
    }

    protected void ToggleWorldFilters()
    {
        showWorldFilters = !showWorldFilters;
    }

    protected void ClearWorldFilters()
    {
        worldQuery = string.Empty;
        worldKind = string.Empty;
        worldType = string.Empty;
        showWorldFilters = false;
        ResetWorldWindow();
    }

    [JSInvokable]
    public Task LoadMoreWorlds()
    {
        worldVisibleCount += ExplorerListBatchSize;
        worldObserverConfigured = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected async Task CreateWorldSpaceHabitatAsync(World world)
    {
        if (spaceHabitatEmpireId == 0)
        {
            spaceHabitatStatus = "Choose an empire.";
            return;
        }

        try
        {
            var habitat = await SpaceHabitatService.CreateOrbitingWorldAsync(
                world.Id,
                spaceHabitatEmpireId,
                spaceHabitatName);

            await LoadSelectedWorkspaceAsync();
            selectedHabitatId = habitat.Id;
            selectedWorldId = 0;
            spaceHabitatName = string.Empty;
            spaceHabitatStatus = $"{habitat.Name} created.";
            await EnsureEntityImagesLoadedAsync();
            NavigateToCurrentSelection(replace: true);
        }
        catch (InvalidOperationException ex)
        {
            spaceHabitatStatus = ex.Message;
        }
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

    protected string DisplayEmpire(int? empireId)
    {
        return empireId is null
            ? "Unassigned"
            : ExplorerEmpires.FirstOrDefault(empire => empire.Id == empireId.Value)?.Name ?? empireId.Value.ToString();
    }

    protected static string DisplayPopulation(long population)
    {
        return population >= 1_000_000_000
            ? $"{population / 1_000_000_000m:0.#} billion"
            : $"{population / 1_000_000m:0.#} million";
    }

    protected static string Format(double? value)
    {
        return value?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
    }

    protected static string DisplayOrbit(World world)
    {
        if (world.OrbitRadiusAu is not null)
        {
            return $"{world.OrbitRadiusAu:0.###} AU";
        }

        if (world.OrbitRadiusKm is not null)
        {
            return $"{world.OrbitRadiusKm:0,0} km";
        }

        return "N/A";
    }

    protected static string DisplayNullable(double? value, string suffix)
    {
        return value is null ? "N/A" : $"{value:0.###}{suffix}";
    }

    protected static string DisplayOrbitTarget(StarSystem? system, SpaceHabitat habitat)
    {
        if (system is null)
        {
            return "Unknown";
        }

        return habitat.OrbitTargetKind switch
        {
            OrbitTargetKind.AstralBody => DisplayAstralBody(system, habitat.OrbitTargetId),
            OrbitTargetKind.World => system.Worlds.FirstOrDefault(world => world.Id == habitat.OrbitTargetId)?.Name ?? $"World {habitat.OrbitTargetId}",
            OrbitTargetKind.SpaceHabitat => $"Habitat {habitat.OrbitTargetId}",
            _ => "Unknown"
        };
    }

    protected static string DisplayAstralBody(StarSystem system, int sequence)
    {
        var body = system.AstralBodies.ElementAtOrDefault(sequence);
        if (body is null)
        {
            return $"Astral body {sequence}";
        }

        return string.IsNullOrWhiteSpace(body.Classification)
            ? body.Role.ToString()
            : $"{body.Role} ({body.Classification})";
    }

    protected static string DisplayNullable(int? value, string suffix)
    {
        return value is null ? "N/A" : $"{value}{suffix}";
    }

    protected static IEnumerable<OrbitalSatellite> GetWorldSatellites(StarSystem? system, World world)
    {
        if (system is null)
        {
            yield break;
        }

        foreach (var satellite in system.Worlds
            .Where(item => item.ParentWorldId == world.Id)
            .OrderBy(item => item.OrbitRadiusKm ?? double.MaxValue)
            .ThenBy(item => item.Name))
        {
            yield return OrbitalSatellite.FromWorld(satellite);
        }

        foreach (var habitat in system.SpaceHabitats
            .Where(item => item.OrbitTargetKind == OrbitTargetKind.World && item.OrbitTargetId == world.Id)
            .OrderBy(item => item.OrbitRadiusKm ?? double.MaxValue)
            .ThenBy(item => item.Name))
        {
            yield return OrbitalSatellite.FromHabitat(habitat);
        }
    }

    private async Task RefreshExplorerShellAsync(int? preferredSectorId = null, CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: preferredSectorId,
            cancellationToken: cancellationToken);
    }

    private async Task LoadSelectedWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        if (selectedSectorId <= 0 || selectedSystemId <= 0)
        {
            selectedWorldsWorkspace = null;
            entityImages = [];
            lastLoadedImageKey = string.Empty;
            return;
        }

        selectedWorldsWorkspace = await ExplorerQueryService.LoadWorldsWorkspaceAsync(selectedSectorId, selectedSystemId, cancellationToken);
        await EnsureEntityImagesLoadedAsync(cancellationToken);
    }

    private async Task<int> ResolveRequestedSystemIdAsync(StarWinSector sector, CancellationToken cancellationToken = default)
    {
        if (RequestedSystemId is int requestedSystemId
            && sector.Systems.Any(system => system.Id == requestedSystemId))
        {
            return requestedSystemId;
        }

        int? resolvedSystemId = null;
        if (RequestedColonyId is int requestedColonyId)
        {
            resolvedSystemId = await ExplorerQueryService.ResolveSystemIdAsync(
                sector.Id,
                colonyId: requestedColonyId,
                cancellationToken: cancellationToken);
        }
        else if (RequestedHabitatId is int requestedHabitatId)
        {
            resolvedSystemId = await ExplorerQueryService.ResolveSystemIdAsync(
                sector.Id,
                habitatId: requestedHabitatId,
                cancellationToken: cancellationToken);
        }
        else if (RequestedWorldId is int requestedWorldId)
        {
            resolvedSystemId = await ExplorerQueryService.ResolveSystemIdAsync(
                sector.Id,
                worldId: requestedWorldId,
                cancellationToken: cancellationToken);
        }

        if (resolvedSystemId is int requestedSelectionSystemId
            && sector.Systems.Any(system => system.Id == requestedSelectionSystemId))
        {
            return requestedSelectionSystemId;
        }

        return ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
    }

    private async Task ApplyRequestedSelectionAsync()
    {
        var system = GetSelectedSystemRecord();
        if (system is null)
        {
            selectedWorldId = 0;
            selectedHabitatId = 0;
            return;
        }

        if (RequestedColonyId is int requestedColonyId)
        {
            var colonyWorld = system.Worlds.FirstOrDefault(world => world.Colony?.Id == requestedColonyId);
            if (colonyWorld is not null)
            {
                selectedWorldId = colonyWorld.Id;
                selectedHabitatId = 0;
                await EnsureEntityImagesLoadedAsync();
                return;
            }
        }

        if (RequestedHabitatId is int requestedHabitatId
            && system.SpaceHabitats.FirstOrDefault(habitat => habitat.Id == requestedHabitatId) is SpaceHabitat habitat)
        {
            selectedHabitatId = habitat.Id;
            selectedWorldId = 0;
            await EnsureEntityImagesLoadedAsync();
            return;
        }

        if (RequestedWorldId is int requestedWorldId
            && system.Worlds.FirstOrDefault(world => world.Id == requestedWorldId) is World world)
        {
            selectedWorldId = world.Id;
            selectedHabitatId = 0;
            await EnsureEntityImagesLoadedAsync();
            return;
        }

        if (selectedHabitatId > 0 && system.SpaceHabitats.Any(item => item.Id == selectedHabitatId))
        {
            selectedWorldId = 0;
            await EnsureEntityImagesLoadedAsync();
            return;
        }

        if (selectedWorldId > 0 && system.Worlds.Any(item => item.Id == selectedWorldId))
        {
            selectedHabitatId = 0;
            await EnsureEntityImagesLoadedAsync();
            return;
        }

        SelectDefaultRecord(system);
        await EnsureEntityImagesLoadedAsync();
    }

    private async Task EnsureEntityImagesLoadedAsync(CancellationToken cancellationToken = default)
    {
        var targets = new List<EntityImageTarget>();
        if (selectedWorldId > 0)
        {
            targets.Add(new EntityImageTarget(EntityImageTargetKind.World, selectedWorldId));
        }

        if (selectedHabitatId > 0)
        {
            targets.Add(new EntityImageTarget(EntityImageTargetKind.SpaceHabitat, selectedHabitatId));
        }

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
        await LoadSelectedWorkspaceAsync();
        await ApplyRequestedSelectionAsync();
        NavigateToCurrentSelection(replace: true);
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
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Worlds")));
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

    private async Task ConfigureWorldObserverAsync()
    {
        if (!worldHasMoreRecords)
        {
            worldObserverConfigured = false;
            if (loadMoreScrollModule is not null)
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "world");
            }

            return;
        }

        if (worldObserverConfigured)
        {
            return;
        }

        loadMoreScrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/timelineInfiniteScroll.js");
        dotNetReference ??= DotNetObjectReference.Create(this);
        await loadMoreScrollModule.InvokeVoidAsync("observeLoadMore", "world", worldLoadMoreElement, dotNetReference, "LoadMoreWorlds");
        worldObserverConfigured = true;
    }

    private void NavigateToCurrentSelection(bool replace = false)
    {
        var targetUri = SectorExplorerRoutes.BuildSectionUri(
            "Worlds",
            selectedSectorId,
            selectedSystemId,
            selectedWorldId,
            0,
            selectedHabitatId);

        NavigationManager.NavigateTo(targetUri, replace: replace);
    }

    private StarSystem? GetSelectedSystemRecord()
    {
        return selectedWorldsWorkspace?.System;
    }

    private void SelectDefaultRecord(StarSystem? system)
    {
        selectedSystemId = system?.Id ?? 0;
        selectedWorldId = system?.Worlds.FirstOrDefault()?.Id ?? 0;
        selectedHabitatId = selectedWorldId == 0
            ? system?.SpaceHabitats.FirstOrDefault()?.Id ?? 0
            : 0;
    }

    private static IEnumerable<ExplorerWorldRecord> GetExplorerWorldRecords(StarSystem system)
    {
        foreach (var world in system.Worlds)
        {
            yield return ExplorerWorldRecord.FromWorld(system, world);
        }

        foreach (var habitat in system.SpaceHabitats)
        {
            yield return ExplorerWorldRecord.FromHabitat(system, habitat);
        }
    }

    private bool ExplorerWorldRecordMatches(ExplorerWorldRecord record)
    {
        if (!string.IsNullOrWhiteSpace(worldKind)
            && !string.Equals(record.Kind, worldKind, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(worldType)
            && !string.Equals(record.Type, worldType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(worldQuery))
        {
            return true;
        }

        var query = worldQuery.Trim();
        return ContainsQuery(record.Name, query)
            || ContainsQuery(record.Kind, query)
            || ContainsQuery(record.Type, query)
            || ContainsQuery(record.System.Name, query)
            || ContainsQuery(record.World?.Id.ToString(), query)
            || ContainsQuery(record.Habitat?.Id.ToString(), query)
            || ContainsQuery(record.World?.LegacyPlanetId?.ToString(), query)
            || ContainsQuery(record.World?.LegacyMoonId?.ToString(), query)
            || ContainsQuery(record.World?.AtmosphereType, query)
            || ContainsQuery(record.World?.AtmosphereComposition, query)
            || ContainsQuery(record.World?.WaterType, query)
            || ContainsQuery(record.World?.Colony?.ColonyClass, query)
            || ContainsQuery(record.World?.Colony?.AllegianceName, query)
            || ContainsQuery(DisplayEmpire(record.Habitat?.ControlledByEmpireId), query);
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

    public async ValueTask DisposeAsync()
    {
        if (loadMoreScrollModule is not null)
        {
            try
            {
                await loadMoreScrollModule.InvokeVoidAsync("disconnectLoadMore", "world");
                await loadMoreScrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }

    protected sealed record OrbitalSatellite(string Name, string Kind, World? World, SpaceHabitat? Habitat)
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
