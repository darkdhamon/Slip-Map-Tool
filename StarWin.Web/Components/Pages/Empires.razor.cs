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
    private const int ExplorerListBatchSize = 120;
    private const int ComboAllFilterId = -1;
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

    [SupplyParameterFromQuery(Name = "empireId")]
    public int? RequestedEmpireId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    protected readonly ExplorerSectorCacheBuilder sectorCacheBuilder = new();
    private readonly Dictionary<int, ExplorerSectorLoadSections> loadedSectorSectionsById = [];
    private readonly Dictionary<int, World> worldsById = [];
    private readonly Dictionary<int, AlienRace> alienRacesById = [];
    private readonly Dictionary<int, Empire> empiresById = [];

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
    protected int empireRaceId = ComboAllFilterId;
    protected int empireVisibleCount = ExplorerListBatchSize;
    protected bool empireHasMoreRecords;
    protected bool showEmpireFilters;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoaded;
    private bool entityImagesLoading;
    private string imageUploadStatus = string.Empty;
    private ElementReference empireLoadMoreElement;
    private DotNetObjectReference<Empires>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool empireObserverConfigured;
    private bool browserSessionReady;
    private bool browserSessionRestored;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<AlienRace> ExplorerAlienRaces => explorerContext.AlienRaces;
    protected IReadOnlyList<Empire> ExplorerEmpires => explorerContext.Empires;

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
        selectedEmpireId = ResolveSelectedEmpireId(initialSector);
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
        selectedEmpireId = ResolveSelectedEmpireId(sector);
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

    protected ExplorerSectorCache GetSectorCache(StarWinSector sector)
    {
        return sectorCacheBuilder.Get(sector);
    }

    protected ExplorerSectorSummary GetSectorSummary(StarWinSector sector)
    {
        return sectorCacheBuilder.GetSummary(sector);
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
        selectedEmpireId = ResolveSelectedEmpireId(sector);
        ClearEmpireFilters();
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

    protected async Task SaveEntityNameAsync(EntityNoteTargetKind targetKind, int targetId, string name)
    {
        await EntityNameService.SaveNameAsync(targetKind, targetId, name);
        await RefreshExplorerDataAsync();
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
    }

    protected IEnumerable<Empire> GetFilteredEmpires(IReadOnlyList<Empire> empires)
    {
        return empires
            .Where(EmpireMatches)
            .OrderBy(empire => empire.Name)
            .ThenBy(empire => empire.Id);
    }

    private bool EmpireMatches(Empire empire)
    {
        if (empireRaceId != ComboAllFilterId
            && !empire.RaceMemberships.Any(membership => membership.RaceId == empireRaceId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(empireQuery))
        {
            return true;
        }

        var query = empireQuery.Trim();
        var homeWorld = FindWorld(empire.Founding.FoundingWorldId);
        var sector = GetSelectedSector();
        return ContainsQuery(empire.Name, query)
            || ContainsQuery(empire.Id.ToString(), query)
            || ContainsQuery(empire.LegacyRaceId?.ToString(), query)
            || ContainsQuery(empire.ExpansionPolicy.ToString(), query)
            || (IsFallenEmpire(empire, sector) && ContainsQuery("Fallen empire", query))
            || ContainsQuery(homeWorld?.Name, query)
            || empire.RaceMemberships.Any(membership =>
                ContainsQuery(DisplayRace(membership.RaceId), query)
                || ContainsQuery(membership.Role.ToString(), query))
            || empire.Contacts.Any(contact =>
                ContainsQuery(contact.Relation, query)
                || ContainsQuery(DisplayEmpire(contact.OtherEmpireId), query));
    }

    protected void ResetEmpireWindow()
    {
        empireVisibleCount = ExplorerListBatchSize;
        empireObserverConfigured = false;
    }

    protected void ToggleEmpireFilters()
    {
        showEmpireFilters = !showEmpireFilters;
    }

    protected void ApplyEmpireRaceFilter()
    {
        empireRaceId = ParseComboId(empireRaceText);
        ResetEmpireWindow();
    }

    protected void ClearEmpireFilters()
    {
        empireQuery = string.Empty;
        empireRaceText = string.Empty;
        empireRaceId = ComboAllFilterId;
        showEmpireFilters = false;
        ResetEmpireWindow();
    }

    [JSInvokable]
    public Task LoadMoreEmpires()
    {
        empireVisibleCount += ExplorerListBatchSize;
        empireObserverConfigured = false;
        StateHasChanged();
        return Task.CompletedTask;
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
        worldsById.Clear();
        alienRacesById.Clear();
        empiresById.Clear();
    }

    private async Task EnsureSectorDataLoadedAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            return;
        }

        const ExplorerSectorLoadSections requiredSections = ExplorerSectorLoadSections.Worlds
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
        worldsById.Clear();
    }

    private int ResolveSelectedEmpireId(StarWinSector sector)
    {
        var sectorSummary = GetSectorSummary(sector);
        if (RequestedEmpireId is int requestedEmpireId && sectorSummary.EmpireIds.Contains(requestedEmpireId))
        {
            return requestedEmpireId;
        }

        if (selectedEmpireId > 0 && sectorSummary.EmpireIds.Contains(selectedEmpireId))
        {
            return selectedEmpireId;
        }

        return ExplorerEmpires.FirstOrDefault(empire => sectorSummary.EmpireIds.Contains(empire.Id))?.Id ?? 0;
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
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        sector = GetSelectedSector();
        selectedSystemId = sector.Systems.Any(system => system.Id == storedSelection.SystemId)
            ? storedSelection.SystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        selectedEmpireId = ResolveSelectedEmpireId(sector);
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

    protected string DisplayRace(int raceId)
    {
        if (alienRacesById.Count == 0)
        {
            foreach (var race in ExplorerAlienRaces)
            {
                alienRacesById[race.Id] = race;
            }
        }

        return alienRacesById.TryGetValue(raceId, out var matchedRace) ? matchedRace.Name : raceId.ToString();
    }

    protected string DisplayEmpire(int? empireId)
    {
        if (empireId is null)
        {
            return "Unassigned";
        }

        if (empiresById.Count == 0)
        {
            foreach (var empire in ExplorerEmpires)
            {
                empiresById[empire.Id] = empire;
            }
        }

        return empiresById.TryGetValue(empireId.Value, out var matchedEmpire) ? matchedEmpire.Name : empireId.Value.ToString();
    }

    protected World? FindWorld(int? worldId)
    {
        if (worldId is null or 0)
        {
            return null;
        }

        if (worldsById.Count == 0)
        {
            foreach (var world in ExplorerSectors.SelectMany(sector => sector.Systems).SelectMany(system => system.Worlds))
            {
                worldsById[world.Id] = world;
            }
        }

        return worldsById.TryGetValue(worldId.Value, out var matchedWorld) ? matchedWorld : null;
    }

    protected IEnumerable<ColonyListing> GetEmpireColonies(Empire empire, StarWinSector sector)
    {
        return sector.Systems.Select(system => new { sector, system })
            .SelectMany(item => item.system.Worlds
                .Where(world => world.Colony is not null)
                .Select(world => new ColonyListing(item.sector, item.system, world, world.Colony!)))
            .Where(listing => listing.Colony.IsControlledBy(empire.Id) || listing.Colony.FoundingEmpireId == empire.Id)
            .OrderByDescending(listing => listing.Colony.EstimatedPopulation)
            .ThenBy(listing => listing.World.Name);
    }

    protected IEnumerable<ColonyListing> GetControlledEmpireColonies(Empire empire, StarWinSector sector)
    {
        return GetEmpireColonies(empire, sector)
            .Where(listing => listing.Colony.IsControlledBy(empire.Id));
    }

    protected bool IsFallenEmpire(Empire empire, StarWinSector sector)
    {
        var colonies = GetEmpireColonies(empire, sector).ToList();
        if (colonies.Any(listing => listing.Colony.IsControlledBy(empire.Id)))
        {
            return false;
        }

        return colonies.Count > 0
            || empire.Planets > 0
            || empire.Moons > 0
            || empire.SpaceHabitats > 0
            || empire.NativePopulationMillions > 0
            || empire.SubjectPopulationMillions > 0;
    }

    protected ColonyListing? GetCapitalColony(Empire empire)
    {
        var sector = GetSelectedSector();
        var colonies = GetEmpireColonies(empire, sector).ToList();
        var homeworldColony = colonies.FirstOrDefault(listing => listing.World.Id == empire.Founding.FoundingWorldId);
        if (homeworldColony is not null && homeworldColony.Colony.IsControlledBy(empire.Id))
        {
            return homeworldColony;
        }

        return colonies.FirstOrDefault(listing => listing.Colony.IsControlledBy(empire.Id)) ?? colonies.FirstOrDefault();
    }

    protected static string DisplayColonyName(Colony colony)
    {
        return string.IsNullOrWhiteSpace(colony.Name) ? colony.ColonyClass : colony.Name;
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
            entityImages = await ImageService.GetImagesAsync();
            entityImagesLoaded = true;
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

    protected static bool ContainsQuery(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
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

    protected static int GetGurpsTechLevel(Empire empire)
    {
        return GurpsTechnologyLevelMapper.GetBaseTechLevel(empire.CivilizationProfile.TechLevel);
    }

    public async ValueTask DisposeAsync()
    {
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

    protected sealed record ColonyListing(StarWinSector Sector, StarSystem System, World World, Colony Colony);
}
