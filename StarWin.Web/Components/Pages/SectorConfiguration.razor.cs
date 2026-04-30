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
    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected ExplorerSectorConfigurationState? selectedConfigurationState;
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

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected int SavedRouteCount => selectedConfigurationState?.SavedRouteCount ?? 0;
    protected SectorHyperlaneNetworkReport SavedRouteReport => selectedConfigurationState?.SavedRouteReport ?? SectorHyperlaneNetworkReport.Empty;

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerShellAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
        LoadSectorConfigurationForm(selectedConfigurationState);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ExplorerSectors.Count == 0)
        {
            return;
        }

        var sectorChanged = false;
        var systemChanged = false;

        var requestedSectorId = RequestedSectorId ?? selectedSectorId;
        if (requestedSectorId != selectedSectorId && ExplorerSectors.Any(sector => sector.Id == requestedSectorId))
        {
            selectedSectorId = requestedSectorId;
            sectorChanged = true;
        }

        var sector = GetSelectedSector();
        var resolvedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        if (resolvedSystemId != selectedSystemId)
        {
            selectedSystemId = resolvedSystemId;
            systemChanged = true;
        }

        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);

        if (sectorChanged)
        {
            await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
            LoadSectorConfigurationForm(selectedConfigurationState);
        }
        else if (systemChanged)
        {
            await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
        }
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

    protected string BuildSectionRoute(string sectionName)
    {
        return SectorExplorerRoutes.BuildSectionUri(sectionName, selectedSectorId);
    }

    protected StarWinSector GetSelectedSector()
    {
        return ExplorerSectors.FirstOrDefault(item => item.Id == selectedSectorId) ?? explorerContext.CurrentSector;
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
        await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
        LoadSectorConfigurationForm(selectedConfigurationState);
        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Configuration", selectedSectorId, selectedSystemId));
    }

    protected async Task HandleSelectedSystemTextChangedAsync(string value)
    {
        selectedSystemText = value;
        var sector = GetSelectedSector();
        var systemId = ParseComboId(selectedSystemText);
        if (systemId > 0 && sector.Systems.Any(system => system.Id == systemId))
        {
            selectedSystemId = systemId;
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Configuration", selectedSectorId, selectedSystemId), replace: true);
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

    protected string GetConfigurationRouteSummary(StarSystem? focusedSystem)
    {
        if (focusedSystem is null)
        {
            return "Select a system to preview connected hyperlanes.";
        }

        if (SavedRouteCount == 0)
        {
            return "Preview available after saving routes for this sector.";
        }

        var touchingRoutes = selectedConfigurationState?.SelectedSystemRouteCount ?? 0;
        return $"{touchingRoutes} saved hyperlane segment{(touchingRoutes == 1 ? string.Empty : "s")} touch the selected system.";
    }

    protected static string DisplayDateTime(DateTime? value)
    {
        return value is DateTime timestamp && timestamp != default
            ? timestamp.ToLocalTime().ToString("MMM d, yyyy h:mm tt")
            : "Not recorded";
    }

    private void LoadSectorConfigurationForm(ExplorerSectorConfigurationState? state)
    {
        if (state is null)
        {
            return;
        }

        sectorName = state.SectorName;
        offLaneMaximumDistanceParsecs = Math.Round(state.Configuration.OffLaneMaximumDistanceParsecs, 3);
        tl9AndBelowMaximumConnectionsPerSystem = state.Configuration.Tl9AndBelowMaximumConnectionsPerSystem;
        additionalCrossEmpireConnectionsPerSystem = state.Configuration.AdditionalCrossEmpireConnectionsPerSystem;
        tl6HyperlaneName = state.Configuration.Tl6HyperlaneName;
        tl6MaximumDistanceParsecs = Math.Round(state.Configuration.Tl6MaximumDistanceParsecs, 3);
        tl6OffLaneSpeedMultiplier = Math.Round(state.Configuration.Tl6OffLaneSpeedMultiplier, 3);
        tl6HyperlaneSpeedModifier = Math.Round(state.Configuration.Tl6HyperlaneSpeedModifier, 3);
        tl7HyperlaneName = state.Configuration.Tl7HyperlaneName;
        tl7MaximumDistanceParsecs = Math.Round(state.Configuration.Tl7MaximumDistanceParsecs, 3);
        tl7OffLaneSpeedMultiplier = Math.Round(state.Configuration.Tl7OffLaneSpeedMultiplier, 3);
        tl7HyperlaneSpeedModifier = Math.Round(state.Configuration.Tl7HyperlaneSpeedModifier, 3);
        tl8HyperlaneName = state.Configuration.Tl8HyperlaneName;
        tl8MaximumDistanceParsecs = Math.Round(state.Configuration.Tl8MaximumDistanceParsecs, 3);
        tl8OffLaneSpeedMultiplier = Math.Round(state.Configuration.Tl8OffLaneSpeedMultiplier, 3);
        tl8HyperlaneSpeedModifier = Math.Round(state.Configuration.Tl8HyperlaneSpeedModifier, 3);
        tl9HyperlaneName = state.Configuration.Tl9HyperlaneName;
        tl9MaximumDistanceParsecs = Math.Round(state.Configuration.Tl9MaximumDistanceParsecs, 3);
        tl9OffLaneSpeedMultiplier = Math.Round(state.Configuration.Tl9OffLaneSpeedMultiplier, 3);
        tl9HyperlaneSpeedModifier = Math.Round(state.Configuration.Tl9HyperlaneSpeedModifier, 3);
        tl10HyperlaneName = state.Configuration.Tl10HyperlaneName;
        tl10MaximumDistanceParsecs = Math.Round(state.Configuration.Tl10MaximumDistanceParsecs, 3);
        tl10OffLaneSpeedMultiplier = Math.Round(state.Configuration.Tl10OffLaneSpeedMultiplier, 3);
        tl10HyperlaneSpeedModifier = Math.Round(state.Configuration.Tl10HyperlaneSpeedModifier, 3);
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

        await RefreshExplorerShellAsync();
        await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
        LoadSectorConfigurationForm(selectedConfigurationState);
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
            await RefreshExplorerShellAsync();
            await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
            LoadSectorConfigurationForm(selectedConfigurationState);
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
            await RefreshExplorerShellAsync();
            await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
            sectorConfigurationStatus = result.Assignments.Count == 0
                ? "No independent colonies needed conversion."
                : $"Created {result.CreatedEmpires.Count:N0} empire{(result.CreatedEmpires.Count == 1 ? string.Empty : "s")} and assigned {result.Assignments.Count:N0} colon{(result.Assignments.Count == 1 ? "y" : "ies")}.";
        }
        catch (InvalidOperationException ex)
        {
            sectorConfigurationStatus = ex.Message;
        }
    }

    private async Task RefreshExplorerShellAsync(CancellationToken cancellationToken = default)
    {
        explorerContext = await ExplorerContextService.LoadShellAsync(
            preferredSectorId: RequestedSectorId ?? selectedSectorId,
            cancellationToken: cancellationToken);
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
        await LoadSelectedConfigurationStateAsync(selectedSectorId, selectedSystemId);
        LoadSectorConfigurationForm(selectedConfigurationState);
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

    private async Task LoadSelectedConfigurationStateAsync(int sectorId, int? systemId = null, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            selectedConfigurationState = null;
            return;
        }

        selectedConfigurationState = await ExplorerQueryService.LoadSectorConfigurationStateAsync(
            sectorId,
            systemId,
            cancellationToken);
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

}
