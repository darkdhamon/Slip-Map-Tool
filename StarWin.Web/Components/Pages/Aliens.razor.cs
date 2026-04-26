using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
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

    [SupplyParameterFromQuery(Name = "raceId")]
    public int? RequestedRaceId { get; set; }

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
    protected int selectedRaceId;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected string raceQuery = string.Empty;
    protected string raceEnvironment = string.Empty;
    protected string raceAppearance = string.Empty;
    protected int raceVisibleCount = ExplorerListBatchSize;
    protected bool raceHasMoreRecords;
    protected bool showRaceFilters;

    private IReadOnlyList<EntityImage> entityImages = [];
    private bool entityImagesLoaded;
    private bool entityImagesLoading;
    private string imageUploadStatus = string.Empty;
    private ElementReference raceLoadMoreElement;
    private DotNetObjectReference<Aliens>? dotNetReference;
    private IJSObjectReference? loadMoreScrollModule;
    private bool raceObserverConfigured;
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
        selectedRaceId = ResolveSelectedRaceId(initialSector);
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
        selectedRaceId = ResolveSelectedRaceId(sector);
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
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        selectedRaceId = ResolveSelectedRaceId(sector);
        ClearRaceFilters();
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
        await EnsureSectorDataLoadedAsync(selectedSectorId);
        selectedSystemText = FormatSelectedSystem(GetSelectedSector(), selectedSystemId);
    }

    protected IEnumerable<AlienRace> GetFilteredRaces(IReadOnlyList<AlienRace> races)
    {
        return races
            .Where(RaceMatches)
            .OrderBy(race => race.Name)
            .ThenBy(race => race.Id);
    }

    protected void ResetRaceWindow()
    {
        raceVisibleCount = ExplorerListBatchSize;
        raceObserverConfigured = false;
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
        ResetRaceWindow();
    }

    [JSInvokable]
    public Task LoadMoreRaces()
    {
        raceVisibleCount += ExplorerListBatchSize;
        raceObserverConfigured = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected IEnumerable<RaceEmpireMembership> GetRaceEmpireMemberships(AlienRace race, IReadOnlyCollection<Empire> sectorEmpires)
    {
        return sectorEmpires
            .SelectMany(empire => empire.RaceMemberships
                .Where(membership => membership.RaceId == race.Id)
                .Select(membership => new RaceEmpireMembership(empire, membership)))
            .OrderByDescending(item => item.Membership.IsPrimary)
            .ThenBy(item => item.Empire.Name);
    }

    protected GurpsTemplate BuildGurpsTemplate(AlienRace race, IReadOnlyCollection<Empire> sectorEmpires)
    {
        var sector = GetSelectedSector();
        var memberships = GetRaceEmpireMemberships(race, sectorEmpires).ToList();
        var primaryEmpire = memberships.FirstOrDefault(item => item.Membership.IsPrimary)?.Empire
            ?? memberships.FirstOrDefault()?.Empire;

        var profile = new AlienRaceExportProfile
        {
            Race = race,
            Empire = primaryEmpire,
            HomeWorld = FindWorld(race.HomePlanetId)
        };

        foreach (var listing in sector.Systems
            .SelectMany(system => system.Worlds)
            .Where(world => world.Colony is not null && world.Colony.Demographics.Any(demographic => demographic.RaceId == race.Id))
            .Select(world => world.Colony!))
        {
            profile.Colonies.Add(listing);
        }

        var builder = new GurpsTemplateBuilder();
        return builder.Build(profile, GurpsTemplateEdition.FourthEdition);
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

    protected IReadOnlyList<EntityImage> GetEntityImages(EntityImageTargetKind targetKind, int targetId)
    {
        return entityImages
            .Where(image => image.TargetKind == targetKind && image.TargetId == targetId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.UploadedAt)
            .ToList();
    }

    protected static string DisplayText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
    }

    protected static IEnumerable<string> GetRaceHighlights(AlienRace race)
    {
        if (!string.IsNullOrWhiteSpace(race.BodyCoverType))
        {
            yield return $"Body cover: {race.BodyCoverType}";
        }

        if (!string.IsNullOrWhiteSpace(race.Reproduction))
        {
            yield return $"Reproduction: {race.Reproduction}";
        }

        if (race.DevotionLevel != AlienDevotionLevel.None)
        {
            yield return $"{race.DevotionLevel} devotion";
        }

        if (race.BiologyProfile.Lifespan > 0)
        {
            yield return $"Lifespan {race.BiologyProfile.Lifespan}";
        }
    }

    protected static int CountGurpsTraits(GurpsTemplate template)
    {
        return template.AttributeModifiers.Count
            + template.SecondaryCharacteristicModifiers.Count
            + template.Advantages.Count
            + template.Disadvantages.Count
            + template.Quirks.Count
            + template.Features.Count
            + template.Skills.Count;
    }

    protected static string DisplayGurpsEdition(GurpsTemplateEdition edition)
    {
        return edition switch
        {
            GurpsTemplateEdition.FourthEdition => "GURPS Fourth Edition",
            GurpsTemplateEdition.LegacyStarWin => "Legacy StarWin",
            _ => edition.ToString()
        };
    }

    protected static string DisplayPoints(int points)
    {
        return points > 0 ? $"+{points} points" : $"{points} points";
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
        var homeWorld = FindWorld(race.HomePlanetId);
        return ContainsQuery(race.Name, query)
            || ContainsQuery(race.Id.ToString(), query)
            || ContainsQuery(race.AppearanceType, query)
            || ContainsQuery(race.BodyChemistry, query)
            || ContainsQuery(race.EnvironmentType, query)
            || ContainsQuery(race.GovernmentType, query)
            || ContainsQuery(race.ReproductionMethod, query)
            || ContainsQuery(race.Diet, query)
            || ContainsQuery(race.Religion, query)
            || ContainsQuery(race.BiologyProfile.Body.ToString(), query)
            || ContainsQuery(race.BiologyProfile.Mind.ToString(), query)
            || ContainsQuery(race.BiologyProfile.Speed.ToString(), query)
            || ContainsQuery(race.BiologyProfile.PsiRating.ToString(), query)
            || ContainsQuery(homeWorld?.Name, query);
    }

    protected static string DisplayTraitName(GurpsTemplateTrait trait)
    {
        return trait.Level == 0 ? trait.Name : $"{trait.Name} {trait.Level:+#;-#;0}";
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

    private int ResolveSelectedRaceId(StarWinSector sector)
    {
        var sectorSummary = GetSectorSummary(sector);
        if (RequestedRaceId is int requestedRaceId && sectorSummary.RaceIds.Contains(requestedRaceId))
        {
            return requestedRaceId;
        }

        if (selectedRaceId > 0 && sectorSummary.RaceIds.Contains(selectedRaceId))
        {
            return selectedRaceId;
        }

        return ExplorerAlienRaces.FirstOrDefault(race => sectorSummary.RaceIds.Contains(race.Id))?.Id ?? 0;
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
        selectedRaceId = ResolveSelectedRaceId(sector);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: selectedRaceId), replace: true);
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

    protected sealed record RaceEmpireMembership(Empire Empire, EmpireRaceMembership Membership);
}
