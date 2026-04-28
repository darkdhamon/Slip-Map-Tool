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

public partial class Worlds : ComponentBase, IAsyncDisposable
{
    private const int ExplorerListBatchSize = 120;
    private const int ComboAllFilterId = -1;
    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinSearchService SearchService { get; set; } = default!;
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
    protected readonly ExplorerSectorCacheBuilder sectorCacheBuilder = new();
    protected readonly Dictionary<int, World> WorldsById = [];
    private readonly Dictionary<int, ExplorerSectorLoadSections> loadedSectorSectionsById = [];

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
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
    protected int worldVisibleCount = ExplorerListBatchSize;
    protected bool worldHasMoreRecords;
    protected bool showWorldFilters;
    protected string imageUploadStatus = string.Empty;
    protected string spaceHabitatName = string.Empty;
    protected int spaceHabitatEmpireId;
    protected string spaceHabitatStatus = string.Empty;
    protected bool IsSpaceHabitatCreateDisabled => spaceHabitatEmpireId == 0;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoaded;
    private bool entityImagesLoading;
    private ElementReference worldLoadMoreElement;
    private DotNetObjectReference<Worlds>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool worldObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<Empire> ExplorerEmpires => explorerContext.Empires;

    private IReadOnlyDictionary<int, Empire> EmpiresById =>
        ExplorerEmpires.ToDictionary(empire => empire.Id);

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        ApplyRequestedSelection(GetSelectedSector());
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
            await EnsureSectorDataLoadedAsync(selectedSectorId);
        }

        ApplyRequestedSelection(GetSelectedSector());
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
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        SelectDefaultRecord(sector);
        ClearWorldFilters();
        await PersistExplorerSessionAsync();
        NavigateToCurrentSelection();
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
            SelectDefaultRecord(sector);
            NavigateToCurrentSelection(replace: true);
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

    protected void SelectWorld(int worldId)
    {
        var sector = GetSelectedSector();
        var system = sector.Systems.FirstOrDefault(item => item.Worlds.Any(world => world.Id == worldId));
        if (system is not null)
        {
            selectedSystemId = system.Id;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        }

        selectedWorldId = worldId;
        selectedHabitatId = 0;
        NavigateToCurrentSelection(replace: true);
        _ = PersistExplorerSessionAsync();
    }

    protected void SelectHabitat(int habitatId)
    {
        var sector = GetSelectedSector();
        var system = sector.Systems.FirstOrDefault(item => item.SpaceHabitats.Any(habitat => habitat.Id == habitatId));
        if (system is not null)
        {
            selectedSystemId = system.Id;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        }

        selectedHabitatId = habitatId;
        selectedWorldId = 0;
        NavigateToCurrentSelection(replace: true);
        _ = PersistExplorerSessionAsync();
    }

    protected void SelectParentOfHabitat(SpaceHabitat habitat)
    {
        var sector = GetSelectedSector();
        var system = sector.Systems.FirstOrDefault(item => item.SpaceHabitats.Any(spaceHabitat => spaceHabitat.Id == habitat.Id))
            ?? sector.Systems.FirstOrDefault(item => item.Id == selectedSystemId);

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
        await RefreshExplorerDataAsync();
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        ApplyRequestedSelection(GetSelectedSector());
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

            if (world.StarSystemId is int systemId)
            {
                AddSpaceHabitatToWorkspace(systemId, habitat);
            }

            CompleteSpaceHabitatCreate(world.StarSystemId ?? selectedSystemId, habitat.Name);
            SelectHabitat(habitat.Id);
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
            : EmpiresById.TryGetValue(empireId.Value, out var empire) ? empire.Name : empireId.Value.ToString();
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

    private void ApplyRequestedSelection(StarWinSector sector)
    {
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);

        if (RequestedHabitatId is int requestedHabitatId)
        {
            var habitatSystem = sector.Systems.FirstOrDefault(system => system.SpaceHabitats.Any(habitat => habitat.Id == requestedHabitatId));
            if (habitatSystem is not null)
            {
                selectedSystemId = habitatSystem.Id;
                selectedHabitatId = requestedHabitatId;
                selectedWorldId = 0;
                selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
                return;
            }
        }

        if (RequestedColonyId is int requestedColonyId)
        {
            var colonyListing = sector.Systems
                .SelectMany(system => system.Worlds.Select(world => new { system, world }))
                .FirstOrDefault(item => item.world.Colony?.Id == requestedColonyId);
            if (colonyListing is not null)
            {
                selectedSystemId = colonyListing.system.Id;
                selectedWorldId = colonyListing.world.Id;
                selectedHabitatId = 0;
                selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
                return;
            }
        }

        if (RequestedWorldId is int requestedWorldId)
        {
            var worldSystem = sector.Systems.FirstOrDefault(system => system.Worlds.Any(world => world.Id == requestedWorldId));
            if (worldSystem is not null)
            {
                selectedSystemId = worldSystem.Id;
                selectedWorldId = requestedWorldId;
                selectedHabitatId = 0;
                selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
                return;
            }
        }

        if (selectedHabitatId > 0
            && sector.Systems.FirstOrDefault(system => system.SpaceHabitats.Any(habitat => habitat.Id == selectedHabitatId)) is StarSystem existingHabitatSystem)
        {
            selectedSystemId = existingHabitatSystem.Id;
            selectedWorldId = 0;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            return;
        }

        if (selectedWorldId > 0
            && sector.Systems.FirstOrDefault(system => system.Worlds.Any(world => world.Id == selectedWorldId)) is StarSystem existingWorldSystem)
        {
            selectedSystemId = existingWorldSystem.Id;
            selectedHabitatId = 0;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            return;
        }

        SelectDefaultRecord(sector);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
    }

    private void SelectDefaultRecord(StarWinSector sector)
    {
        var system = sector.Systems.FirstOrDefault(item => item.Id == selectedSystemId)
            ?? sector.Systems.FirstOrDefault();
        selectedSystemId = system?.Id ?? 0;
        selectedWorldId = system?.Worlds.FirstOrDefault()?.Id ?? 0;
        selectedHabitatId = selectedWorldId == 0
            ? system?.SpaceHabitats.FirstOrDefault()?.Id ?? 0
            : 0;
    }

    private void AddSpaceHabitatToWorkspace(int systemId, SpaceHabitat habitat)
    {
        var sector = GetSelectedSector();
        var system = sector.Systems.FirstOrDefault(item => item.Id == systemId);
        if (system is null || system.SpaceHabitats.Any(item => item.Id == habitat.Id))
        {
            return;
        }

        system.SpaceHabitats.Add(habitat);
        sectorCacheBuilder.Invalidate(sector.Id);
    }

    private void CompleteSpaceHabitatCreate(int systemId, string habitatName)
    {
        selectedSystemId = systemId;
        var sector = GetSelectedSector();
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        spaceHabitatName = string.Empty;
        spaceHabitatStatus = $"{habitatName} created.";
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
            includeReferenceData: true,
            cancellationToken: cancellationToken);

        loadedSectorSectionsById.Clear();
        foreach (var sectorId in ExplorerSectors.Select(sector => sector.Id))
        {
            sectorCacheBuilder.Invalidate(sectorId);
        }
        WorldsById.Clear();
    }

    private async Task EnsureSectorDataLoadedAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return;
        }

        const ExplorerSectorLoadSections requiredSections = ExplorerSectorLoadSections.AstralBodies
            | ExplorerSectorLoadSections.Worlds
            | ExplorerSectorLoadSections.SpaceHabitats
            | ExplorerSectorLoadSections.Colonies
            | ExplorerSectorLoadSections.ColonyDemographics;

        loadedSectorSectionsById.TryGetValue(sectorId, out var loadedSections);
        if ((loadedSections & requiredSections) == requiredSections)
        {
            return;
        }

        var detailedSector = await ExplorerContextService.LoadSectorAsync(sectorId, requiredSections, cancellationToken);
        if (detailedSector is null)
        {
            return;
        }

        var sectors = ExplorerSectors.ToList();
        var sectorIndex = sectors.FindIndex(item => item.Id == detailedSector.Id);
        if (sectorIndex >= 0)
        {
            sectors[sectorIndex] = detailedSector;
        }

        explorerContext = explorerContext with
        {
            Sectors = sectors,
            CurrentSector = explorerContext.CurrentSector.Id == detailedSector.Id ? detailedSector : explorerContext.CurrentSector
        };

        loadedSectorSectionsById[sectorId] = loadedSections | requiredSections;
        sectorCacheBuilder.Invalidate(sectorId);
        WorldsById.Clear();
        foreach (var world in explorerContext.Sectors.SelectMany(sector => sector.Systems).SelectMany(system => system.Worlds))
        {
            WorldsById[world.Id] = world;
        }
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
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        sector = GetSelectedSector();
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        SelectDefaultRecord(sector);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
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
