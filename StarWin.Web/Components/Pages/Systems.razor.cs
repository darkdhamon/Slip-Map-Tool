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

public partial class Systems : ComponentBase
{

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

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    protected readonly ExplorerSectorCacheBuilder sectorCacheBuilder = new();
    private readonly Dictionary<int, ExplorerSectorLoadSections> loadedSectorSectionsById = [];

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string imageUploadStatus = string.Empty;
    protected string spaceHabitatName = string.Empty;
    protected int spaceHabitatEmpireId;
    protected string spaceHabitatStatus = string.Empty;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoaded;
    private bool entityImagesLoading;
    private bool browserSessionReady;
    private bool browserSessionRestored;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<Empire> ExplorerEmpires => explorerContext.Empires;
    protected bool IsSpaceHabitatCreateDisabled => spaceHabitatEmpireId == 0;

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
        initialSector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
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

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
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

        _ = EnsureEntityImagesLoadedAsync(backgroundLoad: true);
    }

    protected ExplorerSectorCache GetSectorCache(StarWinSector sector) => sectorCacheBuilder.Get(sector);

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
        await PersistExplorerSessionAsync();
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, selectedSystemId), replace: true);
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
        await RefreshExplorerDataAsync();
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
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

    protected static string Format(double? value)
    {
        return value?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
    }

    protected static string DisplayPopulation(long population)
    {
        return population >= 1_000_000_000
            ? $"{population / 1_000_000_000m:0.#} billion"
            : $"{population / 1_000_000m:0.#} million";
    }

    protected static string DisplayNullable<T>(T? value, string suffix = "") where T : struct
    {
        return value is null ? "Unknown" : $"{value}{suffix}";
    }

    protected string DisplayAllegiance(ushort allegianceId)
    {
        if (allegianceId == ushort.MaxValue)
        {
            return "Independent";
        }

        return EmpiresById.TryGetValue(allegianceId, out var empire) ? empire.Name : allegianceId.ToString();
    }

    private string DisplayEmpire(int? empireId)
    {
        return empireId is null
            ? "Unassigned"
            : EmpiresById.TryGetValue(empireId.Value, out var empire) ? empire.Name : empireId.Value.ToString();
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
            AddSpaceHabitatToWorkspace(system.Id, habitat);
            CompleteSpaceHabitatCreate(system.Id, habitat.Name);
            NavigateToHabitat(habitat.Id);
        }
        catch (InvalidOperationException ex)
        {
            spaceHabitatStatus = ex.Message;
        }
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
        _ = PersistExplorerSessionAsync();
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
    }

    private async Task EnsureSectorDataLoadedAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return;
        }

        const ExplorerSectorLoadSections requiredSections = ExplorerSectorLoadSections.AstralBodies
            | ExplorerSectorLoadSections.Worlds
            | ExplorerSectorLoadSections.SpaceHabitats;

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
    }

    private void RunSearch()
    {
        searchResults = SearchService.Search(searchQuery)
            .Where(result => result.SectorId is null || result.SectorId == selectedSectorId)
            .ToList();
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
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        sector = GetSelectedSector();
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, selectedSystemId), replace: true);
    }

    private async Task PersistExplorerSessionAsync()
    {
        await ExplorerPageState.PersistSelectionAsync(
            JS,
            browserSessionReady,
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Systems")));
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

    private static bool ContainsQuery(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
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
