using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Timeline : ComponentBase
{
    private const string ExplorerSessionStorageKey = "starforgedAtlas.explorerSelection";

    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinSearchService SearchService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    protected readonly string[] sections = ["Overview", "Timeline", "Configuration", "Hyperlanes", "Systems", "Worlds", "Colonies", "Aliens", "Empires"];
    private readonly Dictionary<int, ExplorerSectorLoadSections> loadedSectorSectionsById = [];

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];

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
        selectedSystemId = ResolveSelectedSystemId(initialSector);
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
        selectedSystemId = ResolveSelectedSystemId(sector);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await RestoreExplorerSessionAsync();
        browserSessionReady = true;
        StateHasChanged();
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
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Timeline", selectedSectorId, selectedSystemId));
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Timeline", selectedSectorId, selectedSystemId), replace: true);
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

    protected Task NavigateToRace(int raceId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Aliens", selectedSectorId, selectedSystemId, raceId: raceId));
        return Task.CompletedTask;
    }

    protected Task NavigateToEmpire(int empireId)
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Empires", selectedSectorId, selectedSystemId, empireId: empireId));
        return Task.CompletedTask;
    }

    protected Task NavigateToColony(int colonyId)
    {
        var sector = GetSelectedSector();
        var listing = sector.Systems
            .SelectMany(system => system.Worlds.Select(world => new { system, world }))
            .FirstOrDefault(item => item.world.Colony?.Id == colonyId);
        if (listing is not null)
        {
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Colonies", selectedSectorId, listing.system.Id, listing.world.Id, colonyId));
        }

        return Task.CompletedTask;
    }

    protected Task NavigateToWorld(int worldId)
    {
        var sector = GetSelectedSector();
        var system = sector.Systems.FirstOrDefault(item => item.Worlds.Any(world => world.Id == worldId));
        var systemId = system?.Id ?? selectedSystemId;
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Worlds", selectedSectorId, systemId, worldId: worldId));
        return Task.CompletedTask;
    }

    protected Task NavigateToSystemRecord(int systemId)
    {
        if (systemId > 0)
        {
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Systems", selectedSectorId, systemId));
        }

        return Task.CompletedTask;
    }

    private async Task RefreshExplorerDataAsync(CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            includeSavedRoutes: false,
            includeReferenceData: true,
            cancellationToken: cancellationToken);

        loadedSectorSectionsById.Clear();
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
    }

    private int ResolveSelectedSystemId(StarWinSector sector)
    {
        if (RequestedSystemId is int requestedSystemId && sector.Systems.Any(system => system.Id == requestedSystemId))
        {
            return requestedSystemId;
        }

        if (selectedSystemId > 0 && sector.Systems.Any(system => system.Id == selectedSystemId))
        {
            return selectedSystemId;
        }

        return sector.Systems.FirstOrDefault()?.Id ?? 0;
    }

    private void RunSearch()
    {
        var sector = GetSelectedSector();
        var sectorRaceIds = sector.Systems
            .SelectMany(system => system.Worlds)
            .Where(world => world.Colony is not null)
            .SelectMany(world => world.Colony!.Demographics.Select(demographic => demographic.RaceId))
            .Concat(sector.Systems.SelectMany(system => system.Worlds)
                .Where(world => world.Colony is not null)
                .Select(world => (int)world.Colony!.RaceId))
            .ToHashSet();
        var sectorEmpireIds = sector.Systems
            .Where(system => system.AllegianceId != ushort.MaxValue)
            .Select(system => (int)system.AllegianceId)
            .Concat(sector.Systems.SelectMany(system => system.Worlds)
                .Where(world => world.Colony is not null)
                .SelectMany(world => new[]
                {
                    world.Colony!.AllegianceId == ushort.MaxValue ? 0 : world.Colony.AllegianceId,
                    world.Colony.ControllingEmpireId is > 0 ? world.Colony.ControllingEmpireId.Value : 0,
                    world.Colony.FoundingEmpireId is > 0 ? world.Colony.FoundingEmpireId.Value : 0,
                    world.Colony.ParentEmpireId is > 0 ? world.Colony.ParentEmpireId.Value : 0
                }))
            .Where(id => id > 0)
            .ToHashSet();

        searchResults = SearchService.Search(searchQuery)
            .Where(result => result.Type switch
            {
                StarWinSearchResultType.AlienRace => result.RaceId is int raceId && sectorRaceIds.Contains(raceId),
                StarWinSearchResultType.Empire => result.EmpireId is int empireId && sectorEmpireIds.Contains(empireId),
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
        string? storedValue;
        try
        {
            storedValue = await JS.InvokeAsync<string?>("sessionStorage.getItem", ExplorerSessionStorageKey);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return;
        }

        ExplorerSessionSelection? storedSelection;
        try
        {
            storedSelection = System.Text.Json.JsonSerializer.Deserialize<ExplorerSessionSelection>(storedValue);
        }
        catch (System.Text.Json.JsonException)
        {
            return;
        }

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
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Timeline", selectedSectorId, selectedSystemId), replace: true);
    }

    private async Task PersistExplorerSessionAsync()
    {
        if (!browserSessionReady)
        {
            return;
        }

        var selection = new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Timeline"));
        var value = System.Text.Json.JsonSerializer.Serialize(selection);
        try
        {
            await JS.InvokeVoidAsync("sessionStorage.setItem", ExplorerSessionStorageKey, value);
        }
        catch (InvalidOperationException)
        {
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
            return 0;
        }

        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        var idText = separatorIndex < 0 ? value : value[..separatorIndex];
        return int.TryParse(idText, out var id) ? id : 0;
    }

    private sealed record ExplorerSessionSelection(int SectorId, int SystemId, bool AutoLoadSectorMap, string SectionSlug);
}
