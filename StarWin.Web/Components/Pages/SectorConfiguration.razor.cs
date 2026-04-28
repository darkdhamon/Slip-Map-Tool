using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Explorer;
using SectorConfigModel = StarWin.Domain.Model.Entity.StarMap.SectorConfiguration;

namespace StarWin.Web.Components.Pages;

public partial class SectorConfiguration : ComponentBase
{
    private static readonly IReadOnlyList<string> routeSaveLoadingSteps =
    [
        "Load the current sector and empire data.",
        "Generate exactly one route per system pair.",
        "Write the refreshed route cache to the database."
    ];

    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected IStarWinSectorConfigurationService SectorConfigurationService { get; set; } = default!;
    [Inject] protected IStarWinSectorRouteService SectorRouteService { get; set; } = default!;
    [Inject] protected IStarWinIndependentColonyService IndependentColonyService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    protected readonly ExplorerSectorCacheBuilder sectorCacheBuilder = new();

    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected ExplorerHyperlaneWorkspace? selectedWorkspace;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];

    protected string sectorName = string.Empty;
    protected decimal offLaneMaximumDistanceParsecs = 2m;
    protected int tl9AndBelowMaximumConnectionsPerSystem = 4;
    protected int additionalCrossEmpireConnectionsPerSystem = 1;
    protected string tl6HyperlaneName = "Basic Hyperlane";
    protected decimal tl6MaximumDistanceParsecs = 1m;
    protected decimal tl6OffLaneSpeedMultiplier = 2m;
    protected decimal tl6HyperlaneSpeedModifier = 2m;
    protected string tl7HyperlaneName = "Enhanced Hyperlane";
    protected decimal tl7MaximumDistanceParsecs = 1.2m;
    protected decimal tl7OffLaneSpeedMultiplier = 4m;
    protected decimal tl7HyperlaneSpeedModifier = 2.25m;
    protected string tl8HyperlaneName = "Advanced Hyperlane";
    protected decimal tl8MaximumDistanceParsecs = 1.4m;
    protected decimal tl8OffLaneSpeedMultiplier = 8m;
    protected decimal tl8HyperlaneSpeedModifier = 2.5m;
    protected string tl9HyperlaneName = "Prime Hyperlane";
    protected decimal tl9MaximumDistanceParsecs = 1.6m;
    protected decimal tl9OffLaneSpeedMultiplier = 16m;
    protected decimal tl9HyperlaneSpeedModifier = 2.75m;
    protected string tl10HyperlaneName = "Ascendant Hyperlane";
    protected decimal tl10MaximumDistanceParsecs = -1m;
    protected decimal tl10OffLaneSpeedMultiplier = 32m;
    protected decimal tl10HyperlaneSpeedModifier = 3m;
    protected string sectorConfigurationStatus = string.Empty;
    protected bool routeSaveLoadingVisible;
    protected string routeSaveLoadingStatus = string.Empty;
    protected string routeSaveLoadingDetail = string.Empty;
    protected int routeSaveLoadingPercent;
    protected int? routeSaveProcessedItems;
    protected int? routeSaveTotalItems;

    private bool browserSessionReady;
    private bool browserSessionRestored;
    private StarWinSector? selectedSectorRecord;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        LoadSectorConfigurationForm(initialSector);
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
            await LoadSelectedWorkspaceAsync(selectedSectorId);
            LoadSectorConfigurationForm(GetSelectedSector());
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
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

    protected ExplorerSectorCache GetSectorCache(StarWinSector sector) => sectorCacheBuilder.Get(sector);

    protected string BuildSectionRoute(string sectionName)
    {
        return SectorExplorerRoutes.BuildSectionUri(sectionName, selectedSectorId);
    }

    protected StarWinSector GetSelectedSector()
    {
        return selectedSectorRecord?.Id == selectedSectorId
            ? selectedSectorRecord
            : ExplorerSectors.FirstOrDefault(item => item.Id == selectedSectorId) ?? explorerContext.CurrentSector;
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
        await LoadSelectedWorkspaceAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        LoadSectorConfigurationForm(sector);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Configuration", selectedSectorId, selectedSystemId));
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
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Configuration", selectedSectorId, selectedSystemId), replace: true);
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

    protected SectorHyperlaneNetworkReport GetSavedHyperlaneReport(StarWinSector sector)
    {
        return selectedWorkspace is null
            ? SectorHyperlaneNetworkReport.Empty
            : SectorRoutePlanner.BuildHyperlaneNetworkReport(
            selectedWorkspace.EligibleSystemIds,
            sector.SavedRoutes.Select(route => new SectorHyperlaneRouteDefinition(
                route.SourceSystemId,
                route.TargetSystemId,
                (double)route.DistanceParsecs,
                (double)route.TravelTimeYears,
                route.TechnologyLevel,
                route.TierName,
                route.PrimaryOwnerEmpireId,
                route.PrimaryOwnerEmpireName,
                route.SecondaryOwnerEmpireId,
                route.SecondaryOwnerEmpireName)));
    }

    protected string GetConfigurationRouteSummary(StarWinSector sector, StarSystem? focusedSystem)
    {
        if (focusedSystem is null)
        {
            return "Select a system to preview connected hyperlanes.";
        }

        if (sector.SavedRoutes.Count == 0)
        {
            return "Preview available after saving routes for this sector.";
        }

        var touchingRoutes = sector.SavedRoutes.Count(route =>
            route.SourceSystemId == focusedSystem.Id || route.TargetSystemId == focusedSystem.Id);
        return $"{touchingRoutes} saved hyperlane segment{(touchingRoutes == 1 ? string.Empty : "s")} touch the selected system.";
    }

    protected static string DisplayDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
    }

    private void LoadSectorConfigurationForm(StarWinSector sector)
    {
        sectorName = sector.Name;
        offLaneMaximumDistanceParsecs = Math.Round(sector.Configuration.OffLaneMaximumDistanceParsecs, 3);
        tl9AndBelowMaximumConnectionsPerSystem = sector.Configuration.Tl9AndBelowMaximumConnectionsPerSystem;
        additionalCrossEmpireConnectionsPerSystem = sector.Configuration.AdditionalCrossEmpireConnectionsPerSystem;
        tl6HyperlaneName = sector.Configuration.Tl6HyperlaneName;
        tl6MaximumDistanceParsecs = Math.Round(sector.Configuration.Tl6MaximumDistanceParsecs, 3);
        tl6OffLaneSpeedMultiplier = Math.Round(sector.Configuration.Tl6OffLaneSpeedMultiplier, 3);
        tl6HyperlaneSpeedModifier = Math.Round(sector.Configuration.Tl6HyperlaneSpeedModifier, 3);
        tl7HyperlaneName = sector.Configuration.Tl7HyperlaneName;
        tl7MaximumDistanceParsecs = Math.Round(sector.Configuration.Tl7MaximumDistanceParsecs, 3);
        tl7OffLaneSpeedMultiplier = Math.Round(sector.Configuration.Tl7OffLaneSpeedMultiplier, 3);
        tl7HyperlaneSpeedModifier = Math.Round(sector.Configuration.Tl7HyperlaneSpeedModifier, 3);
        tl8HyperlaneName = sector.Configuration.Tl8HyperlaneName;
        tl8MaximumDistanceParsecs = Math.Round(sector.Configuration.Tl8MaximumDistanceParsecs, 3);
        tl8OffLaneSpeedMultiplier = Math.Round(sector.Configuration.Tl8OffLaneSpeedMultiplier, 3);
        tl8HyperlaneSpeedModifier = Math.Round(sector.Configuration.Tl8HyperlaneSpeedModifier, 3);
        tl9HyperlaneName = sector.Configuration.Tl9HyperlaneName;
        tl9MaximumDistanceParsecs = Math.Round(sector.Configuration.Tl9MaximumDistanceParsecs, 3);
        tl9OffLaneSpeedMultiplier = Math.Round(sector.Configuration.Tl9OffLaneSpeedMultiplier, 3);
        tl9HyperlaneSpeedModifier = Math.Round(sector.Configuration.Tl9HyperlaneSpeedModifier, 3);
        tl10HyperlaneName = sector.Configuration.Tl10HyperlaneName;
        tl10MaximumDistanceParsecs = Math.Round(sector.Configuration.Tl10MaximumDistanceParsecs, 3);
        tl10OffLaneSpeedMultiplier = Math.Round(sector.Configuration.Tl10OffLaneSpeedMultiplier, 3);
        tl10HyperlaneSpeedModifier = Math.Round(sector.Configuration.Tl10HyperlaneSpeedModifier, 3);
        sectorConfigurationStatus = string.Empty;
    }

    private async Task SaveSectorConfigurationAsync()
    {
        var sector = GetSelectedSector();
        string savedSectorName;
        try
        {
            savedSectorName = await SectorConfigurationService.SaveSectorNameAsync(sector.Id, sectorName);
        }
        catch (InvalidOperationException exception)
        {
            sectorConfigurationStatus = exception.Message;
            return;
        }

        var configuration = await SectorConfigurationService.SaveHyperlaneSettingsAsync(
            sector.Id,
            new SectorConfigModel
            {
                SectorId = sector.Id,
                OffLaneMaximumDistanceParsecs = offLaneMaximumDistanceParsecs,
                Tl9AndBelowMaximumConnectionsPerSystem = tl9AndBelowMaximumConnectionsPerSystem,
                AdditionalCrossEmpireConnectionsPerSystem = additionalCrossEmpireConnectionsPerSystem,
                Tl6HyperlaneName = tl6HyperlaneName,
                Tl6MaximumDistanceParsecs = tl6MaximumDistanceParsecs,
                Tl6OffLaneSpeedMultiplier = tl6OffLaneSpeedMultiplier,
                Tl6HyperlaneSpeedModifier = tl6HyperlaneSpeedModifier,
                Tl7HyperlaneName = tl7HyperlaneName,
                Tl7MaximumDistanceParsecs = tl7MaximumDistanceParsecs,
                Tl7OffLaneSpeedMultiplier = tl7OffLaneSpeedMultiplier,
                Tl7HyperlaneSpeedModifier = tl7HyperlaneSpeedModifier,
                Tl8HyperlaneName = tl8HyperlaneName,
                Tl8MaximumDistanceParsecs = tl8MaximumDistanceParsecs,
                Tl8OffLaneSpeedMultiplier = tl8OffLaneSpeedMultiplier,
                Tl8HyperlaneSpeedModifier = tl8HyperlaneSpeedModifier,
                Tl9HyperlaneName = tl9HyperlaneName,
                Tl9MaximumDistanceParsecs = tl9MaximumDistanceParsecs,
                Tl9OffLaneSpeedMultiplier = tl9OffLaneSpeedMultiplier,
                Tl9HyperlaneSpeedModifier = tl9HyperlaneSpeedModifier,
                Tl10HyperlaneName = tl10HyperlaneName,
                Tl10MaximumDistanceParsecs = tl10MaximumDistanceParsecs,
                Tl10OffLaneSpeedMultiplier = tl10OffLaneSpeedMultiplier,
                Tl10HyperlaneSpeedModifier = tl10HyperlaneSpeedModifier
            });

        await RefreshExplorerDataAsync();
        var reloadedSector = GetSelectedSector();
        LoadSectorConfigurationForm(reloadedSector);
        sectorConfigurationStatus = $"{savedSectorName} saved. Off-lane distance is {configuration.OffLaneMaximumDistanceParsecs:0.###} parsecs, and TL6-TL10 travel tiers now use the current sector configuration.";
    }

    private async Task SaveCurrentRoutesAsync()
    {
        if (routeSaveLoadingVisible)
        {
            return;
        }

        var sector = GetSelectedSector();
        routeSaveLoadingVisible = true;
        routeSaveLoadingStatus = "Loading sector";
        routeSaveLoadingDetail = "Preparing to refresh the route cache for this sector.";
        routeSaveLoadingPercent = 5;
        routeSaveProcessedItems = null;
        routeSaveTotalItems = null;
        StateHasChanged();
        await Task.Yield();

        var progress = new Progress<SectorRouteSaveProgress>(update =>
        {
            routeSaveLoadingStatus = update.Status;
            routeSaveLoadingDetail = update.Detail;
            routeSaveLoadingPercent = Math.Clamp(update.Percent, 0, 100);
            routeSaveProcessedItems = update.ProcessedItems;
            routeSaveTotalItems = update.TotalItems;
            InvokeAsync(StateHasChanged);
        });

        try
        {
            var result = await SectorRouteService.SaveCurrentRoutesAsync(sector.Id, progress);
            routeSaveLoadingStatus = "Refreshing explorer";
            routeSaveLoadingDetail = "Reloading the current sector so the updated routes are ready to use.";
            routeSaveLoadingPercent = 100;
            routeSaveProcessedItems = null;
            routeSaveTotalItems = null;
            await RefreshExplorerDataAsync();
            LoadSectorConfigurationForm(GetSelectedSector());
            sectorConfigurationStatus = result.ReplacedExistingRoutes
                ? $"Updated {result.RouteCount:N0} saved hyperlane segment{(result.RouteCount == 1 ? string.Empty : "s")} for {sector.Name}. {result.GeneratedRouteCount:N0} regenerated, {result.PreservedUserRouteCount:N0} user-persisted kept, {result.NetworkReport.DistinctNetworkCount:N0} network{(result.NetworkReport.DistinctNetworkCount == 1 ? string.Empty : "s")}, {result.NetworkReport.StrandedSystemCount:N0} stranded system{(result.NetworkReport.StrandedSystemCount == 1 ? string.Empty : "s")}."
                : $"Saved {result.RouteCount:N0} hyperlane segment{(result.RouteCount == 1 ? string.Empty : "s")} for {sector.Name}. {result.NetworkReport.DistinctNetworkCount:N0} network{(result.NetworkReport.DistinctNetworkCount == 1 ? string.Empty : "s")} and {result.NetworkReport.StrandedSystemCount:N0} stranded system{(result.NetworkReport.StrandedSystemCount == 1 ? string.Empty : "s")}.";
        }
        catch (Exception ex)
        {
            sectorConfigurationStatus = $"Route save failed: {ex.GetBaseException().Message}";
        }
        finally
        {
            routeSaveLoadingVisible = false;
            routeSaveLoadingStatus = string.Empty;
            routeSaveLoadingDetail = string.Empty;
            routeSaveLoadingPercent = 0;
            routeSaveProcessedItems = null;
            routeSaveTotalItems = null;
            StateHasChanged();
        }
    }

    private async Task ConvertIndependentColoniesAsync(int sectorId)
    {
        sectorConfigurationStatus = "Converting independent colonies...";
        try
        {
            var result = await IndependentColonyService.ConvertIndependentColoniesAsync(sectorId);
            await RefreshExplorerDataAsync();
            sectorConfigurationStatus = result.Assignments.Count == 0
                ? "No independent colonies needed conversion."
                : $"Created {result.CreatedEmpires.Count:N0} empire{(result.CreatedEmpires.Count == 1 ? string.Empty : "s")} and assigned {result.Assignments.Count:N0} colon{(result.Assignments.Count == 1 ? "y" : "ies")}.";
        }
        catch (InvalidOperationException ex)
        {
            sectorConfigurationStatus = ex.Message;
        }
    }

    private async Task RefreshExplorerDataAsync(CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: RequestedSectorId ?? selectedSectorId,
            cancellationToken: cancellationToken);

        var workspaceSectorId = RequestedSectorId ?? selectedSectorId;
        if (workspaceSectorId <= 0)
        {
            workspaceSectorId = explorerContext.CurrentSector.Id;
        }

        await LoadSelectedWorkspaceAsync(workspaceSectorId, cancellationToken);
        foreach (var sectorId in ExplorerSectors.Select(sector => sector.Id))
        {
            sectorCacheBuilder.Invalidate(sectorId);
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
        LoadSectorConfigurationForm(sector);
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Configuration", selectedSectorId, selectedSystemId), replace: true);
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
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Configuration")));
    }

    private async Task LoadSelectedWorkspaceAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            selectedWorkspace = null;
            selectedSectorRecord = null;
            return;
        }

        selectedWorkspace = await ExplorerQueryService.LoadHyperlaneWorkspaceAsync(sectorId, cancellationToken);
        selectedSectorRecord = selectedWorkspace is null ? null : BuildSectorRecord(selectedWorkspace);
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

    private static StarWinSector BuildSectorRecord(ExplorerHyperlaneWorkspace workspace)
    {
        var sector = new StarWinSector
        {
            Id = workspace.SectorId,
            Name = workspace.SectorName,
            Configuration = workspace.Configuration
        };

        foreach (var system in workspace.Systems)
        {
            sector.Systems.Add(new StarSystem
            {
                Id = system.SystemId,
                LegacySystemId = system.LegacySystemId,
                SectorId = workspace.SectorId,
                Name = system.Name,
                Coordinates = system.Coordinates,
                AllegianceId = system.AllegianceId
            });
        }

        foreach (var route in workspace.SavedRoutes)
        {
            sector.SavedRoutes.Add(route);
        }

        return sector;
    }
}
