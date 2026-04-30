using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Explorer;
using SectorConfigModel = StarWin.Domain.Model.Entity.StarMap.SectorConfiguration;

namespace StarWin.Web.Components.Pages;

public partial class Hyperlanes : ComponentBase
{
    private const int ExplorerListBatchSize = 120;
    private static readonly IReadOnlyList<string> routeSaveLoadingSteps =
    [
        "Load the current sector and empire data.",
        "Generate exactly one route per system pair.",
        "Write the refreshed route cache to the database."
    ];

    [Inject] protected IStarWinExplorerContextService ExplorerContextService { get; set; } = default!;
    [Inject] protected IStarWinExplorerQueryService ExplorerQueryService { get; set; } = default!;
    [Inject] protected IStarWinSectorRouteService SectorRouteService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "sectorId")]
    public int? RequestedSectorId { get; set; }

    [SupplyParameterFromQuery(Name = "systemId")]
    public int? RequestedSystemId { get; set; }

    [SupplyParameterFromQuery(Name = "hyperlaneId")]
    public int? RequestedHyperlaneId { get; set; }

    protected static readonly IReadOnlyList<string> sections = SectorExplorerSections.All;
    protected StarWinExplorerContext explorerContext = StarWinExplorerContext.Empty;
    protected ExplorerHyperlaneSetupState? hyperlaneSetupState;
    protected ExplorerHyperlanePageState? selectedHyperlaneState;
    protected string explorerRenderError = string.Empty;
    protected int selectedSectorId;
    protected int selectedSystemId;
    protected string selectedSystemText = string.Empty;
    protected string searchQuery = string.Empty;
    protected IReadOnlyList<StarWinSearchResult> searchResults = [];
    protected int selectedHyperlaneId;
    protected int hyperlaneVisibleCount = ExplorerListBatchSize;
    protected decimal hyperlaneDistanceParsecs = 1m;
    protected decimal hyperlaneTravelTimeYears = 1m;
    protected int hyperlaneSourceSystemId;
    protected int hyperlaneTargetSystemId;
    protected int hyperlaneTechnologyLevel = 6;
    protected string hyperlaneTierName = "Basic Hyperlane";
    protected int hyperlanePrimaryOwnerEmpireId;
    protected int hyperlaneSecondaryOwnerEmpireId;
    protected bool hyperlaneIsUserPersisted;
    protected string hyperlaneStatus = string.Empty;
    protected bool routeSaveLoadingVisible;
    protected string routeSaveLoadingStatus = string.Empty;
    protected string routeSaveLoadingDetail = string.Empty;
    protected int routeSaveLoadingPercent;
    protected int? routeSaveProcessedItems;
    protected int? routeSaveTotalItems;

    private bool browserSessionReady;
    private bool browserSessionRestored;
    private string pendingHyperlaneStatus = string.Empty;
    private IReadOnlyList<SectorSavedRoute> orderedSavedRoutes = [];
    private IReadOnlyDictionary<int, ExplorerHyperlaneSystem> hyperlaneSystemsById = new Dictionary<int, ExplorerHyperlaneSystem>();
    private IReadOnlyDictionary<int, string> hyperlaneSystemNamesById = new Dictionary<int, string>();

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerLookupOption> ExplorerEmpires => selectedHyperlaneState?.Empires ?? [];
    protected IReadOnlyList<ExplorerHyperlaneSystem> ExplorerHyperlaneSystems => selectedHyperlaneState?.Systems ?? [];
    protected IReadOnlyList<SectorSavedRoute> SavedHyperlanes => selectedHyperlaneState?.SavedRoutes ?? [];
    protected SectorHyperlaneNetworkReport SavedHyperlaneReport => selectedHyperlaneState?.SavedRouteReport ?? SectorHyperlaneNetworkReport.Empty;
    protected bool HasSavedHyperlanes => (hyperlaneSetupState?.SavedRouteCount ?? 0) > 0;
    protected SectorConfigModel HyperlaneConfiguration => hyperlaneSetupState?.Configuration ?? new SectorConfigModel();

    private IReadOnlyDictionary<int, string> EmpireNamesById =>
        ExplorerEmpires.ToDictionary(empire => empire.Id, empire => empire.Name);

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        if (HasSavedHyperlanes)
        {
            LoadHyperlaneForm(ResolveSelectedHyperlane());
        }
        else
        {
            ClearSelectedHyperlane();
        }
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
            selectedHyperlaneId = 0;
            await LoadSelectedHyperlaneDataAsync(selectedSectorId);
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        if (HasSavedHyperlanes)
        {
            LoadHyperlaneForm(ResolveSelectedHyperlane());
        }
        else
        {
            ClearSelectedHyperlane();
        }

        ApplyPendingHyperlaneStatus();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RestoreExplorerSessionAsync();
            browserSessionReady = true;
            StateHasChanged();
        }
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
        selectedHyperlaneId = 0;
        await LoadSelectedHyperlaneDataAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        hyperlaneVisibleCount = ExplorerListBatchSize;
        if (HasSavedHyperlanes)
        {
            LoadHyperlaneForm(ResolveSelectedHyperlane());
        }
        else
        {
            ClearSelectedHyperlane();
        }

        await PersistExplorerSessionAsync();
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Hyperlanes", selectedSectorId, selectedSystemId));
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
            if (HasSavedHyperlanes && selectedHyperlaneId == 0)
            {
                StartNewHyperlane();
            }

            NavigationManager.NavigateTo(
                SectorExplorerRoutes.BuildSectionUri(
                    "Hyperlanes",
                    selectedSectorId,
                    selectedSystemId,
                    hyperlaneId: selectedHyperlaneId),
                replace: true);
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

    protected IReadOnlyList<SectorSavedRoute> GetOrderedRoutes()
    {
        return orderedSavedRoutes;
    }

    protected SectorSavedRoute? GetSelectedRoute(IReadOnlyList<SectorSavedRoute>? orderedRoutes = null)
    {
        orderedRoutes ??= GetOrderedRoutes();
        return orderedRoutes.FirstOrDefault(route => route.Id == selectedHyperlaneId);
    }

    protected string FormatHyperlaneOwnerSummary(SectorSavedRoute route)
    {
        if (string.IsNullOrWhiteSpace(route.PrimaryOwnerEmpireName)
            && string.IsNullOrWhiteSpace(route.SecondaryOwnerEmpireName))
        {
            return "No recorded owner metadata";
        }

        if (string.IsNullOrWhiteSpace(route.SecondaryOwnerEmpireName))
        {
            return route.PrimaryOwnerEmpireName;
        }

        return $"{route.PrimaryOwnerEmpireName} + {route.SecondaryOwnerEmpireName}";
    }

    protected static string FormatTravelTimeBreakdown(decimal travelTimeYears)
    {
        const int hoursPerDay = 24;
        const int daysPerYear = 365;
        const int monthsPerYear = 12;
        const int hoursPerYear = daysPerYear * hoursPerDay;
        const int hoursPerMonth = hoursPerYear / monthsPerYear;

        var totalHours = (int)Math.Round(travelTimeYears * hoursPerYear, MidpointRounding.AwayFromZero);
        var years = totalHours / hoursPerYear;
        totalHours %= hoursPerYear;

        var months = totalHours / hoursPerMonth;
        totalHours %= hoursPerMonth;

        var days = totalHours / hoursPerDay;
        var hours = totalHours % hoursPerDay;
        var components = new List<string>();
        if (years > 0)
        {
            components.Add($"{years} Years");
        }

        if (months > 0)
        {
            components.Add($"{months} Months");
        }

        if (days > 0)
        {
            components.Add($"{days} Days");
        }

        if (hours > 0)
        {
            components.Add($"{hours} Hours");
        }

        return components.Count == 0
            ? "0 Hours"
            : string.Join(", ", components);
    }

    protected Task LoadMoreHyperlanes()
    {
        hyperlaneVisibleCount += ExplorerListBatchSize;
        return Task.CompletedTask;
    }

    protected void NavigateToConfiguration()
    {
        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Configuration", selectedSectorId, selectedSystemId));
    }

    protected void SelectHyperlane(int routeId)
    {
        if (routeId <= 0)
        {
            return;
        }

        if (!HasSavedHyperlanes)
        {
            return;
        }

        var route = SavedHyperlanes.FirstOrDefault(item => item.Id == routeId);
        if (route is null)
        {
            return;
        }

        LoadHyperlaneForm(route);
        NavigationManager.NavigateTo(
            SectorExplorerRoutes.BuildSectionUri(
                "Hyperlanes",
                selectedSectorId,
                selectedSystemId,
                hyperlaneId: route.Id),
            replace: true);
    }

    protected void StartNewHyperlane()
    {
        var systems = ExplorerHyperlaneSystems;
        selectedHyperlaneId = 0;
        hyperlaneStatus = string.Empty;
        hyperlaneSourceSystemId = systems.Any(system => system.SystemId == selectedSystemId)
            ? selectedSystemId
            : systems.FirstOrDefault()?.SystemId ?? 0;
        hyperlaneTargetSystemId = systems
            .Where(system => system.SystemId != hyperlaneSourceSystemId)
            .OrderBy(system => system.Name)
            .ThenBy(system => system.SystemId)
            .Select(system => system.SystemId)
            .FirstOrDefault();
        hyperlaneTechnologyLevel = 6;
        hyperlaneTierName = SectorRoutePlanner.GetTierName(HyperlaneConfiguration, hyperlaneTechnologyLevel);
        hyperlanePrimaryOwnerEmpireId = 0;
        hyperlaneSecondaryOwnerEmpireId = 0;
        hyperlaneIsUserPersisted = true;
        RecalculateHyperlaneTravelDefaults();
    }

    protected void BeginNewHyperlane()
    {
        StartNewHyperlane();
        NavigationManager.NavigateTo(
            SectorExplorerRoutes.BuildSectionUri("Hyperlanes", selectedSectorId, selectedSystemId),
            replace: true);
    }

    protected async Task GenerateHyperlanesAsync()
    {
        if (routeSaveLoadingVisible)
        {
            return;
        }

        routeSaveLoadingVisible = true;
        routeSaveLoadingStatus = "Loading sector";
        routeSaveLoadingDetail = "Preparing to generate the saved hyperlane cache for this sector.";
        routeSaveLoadingPercent = 5;
        routeSaveProcessedItems = null;
        routeSaveTotalItems = null;
        hyperlaneStatus = string.Empty;
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
            var sectorId = selectedSectorId > 0
                ? selectedSectorId
                : hyperlaneSetupState?.SectorId ?? explorerContext.CurrentSector.Id;
            var sectorName = hyperlaneSetupState?.SectorName ?? GetSelectedSector().Name;
            var result = await SectorRouteService.SaveCurrentRoutesAsync(sectorId, progress);
            routeSaveLoadingStatus = "Refreshing explorer";
            routeSaveLoadingDetail = "Reloading the current sector so the generated hyperlanes are ready to use.";
            routeSaveLoadingPercent = 100;
            routeSaveProcessedItems = null;
            routeSaveTotalItems = null;

            await RefreshExplorerDataAsync();

            var sector = GetSelectedSector();
            selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
            selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
            hyperlaneVisibleCount = ExplorerListBatchSize;
            if (HasSavedHyperlanes)
            {
                LoadHyperlaneForm(ResolveSelectedHyperlane());
            }

            hyperlaneStatus = result.ReplacedExistingRoutes
                ? $"Updated {result.RouteCount:N0} saved hyperlane segment{(result.RouteCount == 1 ? string.Empty : "s")} for {sectorName}. {result.GeneratedRouteCount:N0} regenerated, {result.PreservedUserRouteCount:N0} user-persisted kept, {result.NetworkReport.DistinctNetworkCount:N0} network{(result.NetworkReport.DistinctNetworkCount == 1 ? string.Empty : "s")}, {result.NetworkReport.StrandedSystemCount:N0} stranded system{(result.NetworkReport.StrandedSystemCount == 1 ? string.Empty : "s")}."
                : $"Saved {result.RouteCount:N0} hyperlane segment{(result.RouteCount == 1 ? string.Empty : "s")} for {sectorName}. {result.NetworkReport.DistinctNetworkCount:N0} network{(result.NetworkReport.DistinctNetworkCount == 1 ? string.Empty : "s")} and {result.NetworkReport.StrandedSystemCount:N0} stranded system{(result.NetworkReport.StrandedSystemCount == 1 ? string.Empty : "s")}.";
        }
        catch (Exception ex)
        {
            hyperlaneStatus = $"Hyperlane generation failed: {ex.GetBaseException().Message}";
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

    protected void RecalculateHyperlaneTravelDefaults()
    {
        if (hyperlaneSourceSystemId <= 0 || hyperlaneTargetSystemId <= 0)
        {
            return;
        }

        if (!hyperlaneSystemsById.TryGetValue(hyperlaneSourceSystemId, out var source)
            || !hyperlaneSystemsById.TryGetValue(hyperlaneTargetSystemId, out var target))
        {
            return;
        }

        if (source is null || target is null)
        {
            return;
        }

        hyperlaneTierName = SectorRoutePlanner.GetTierName(HyperlaneConfiguration, hyperlaneTechnologyLevel);
        hyperlaneDistanceParsecs = decimal.Round((decimal)SectorRoutePlanner.CalculateParsecDistance(source.Coordinates, target.Coordinates), 3);
        hyperlaneTravelTimeYears = decimal.Round((decimal)SectorRoutePlanner.CalculateHyperlaneTravelTimeYears(
            HyperlaneConfiguration,
            hyperlaneTechnologyLevel,
            (double)hyperlaneDistanceParsecs), 6);
    }

    protected async Task SaveHyperlaneAsync()
    {
        var sector = GetSelectedSector();
        var isUpdate = selectedHyperlaneId > 0;
        try
        {
            hyperlaneStatus = "Saving hyperlane...";
            var savedRoute = await SectorRouteService.SaveManualRouteAsync(new SectorManualRouteSaveRequest(
                sector.Id,
                selectedHyperlaneId > 0 ? selectedHyperlaneId : null,
                hyperlaneSourceSystemId,
                hyperlaneTargetSystemId,
                hyperlaneDistanceParsecs,
                hyperlaneTravelTimeYears,
                (byte)hyperlaneTechnologyLevel,
                hyperlaneTierName,
                hyperlanePrimaryOwnerEmpireId > 0 ? hyperlanePrimaryOwnerEmpireId : null,
                hyperlanePrimaryOwnerEmpireId > 0 && EmpireNamesById.TryGetValue(hyperlanePrimaryOwnerEmpireId, out var primaryOwnerName) ? primaryOwnerName : string.Empty,
                hyperlaneSecondaryOwnerEmpireId > 0 ? hyperlaneSecondaryOwnerEmpireId : null,
                hyperlaneSecondaryOwnerEmpireId > 0 && EmpireNamesById.TryGetValue(hyperlaneSecondaryOwnerEmpireId, out var secondaryOwnerName) ? secondaryOwnerName : string.Empty,
                hyperlaneIsUserPersisted));

            await RefreshExplorerDataAsync();

            var reloadedSector = GetSelectedSector();
            selectedSystemText = FormatSelectedSystem(reloadedSector, selectedSystemId);
            var reloadedRoute = SavedHyperlanes.FirstOrDefault(route => route.Id == savedRoute.Id)
                ?? SavedHyperlanes.FirstOrDefault(route =>
                    GetRouteKey(route.SourceSystemId, route.TargetSystemId)
                    == GetRouteKey(savedRoute.SourceSystemId, savedRoute.TargetSystemId));

            LoadHyperlaneForm(reloadedRoute);
            pendingHyperlaneStatus = isUpdate
                ? "Saved hyperlane changes."
                : "Created saved hyperlane.";
            NavigationManager.NavigateTo(
                SectorExplorerRoutes.BuildSectionUri(
                    "Hyperlanes",
                    selectedSectorId,
                    selectedSystemId,
                    hyperlaneId: selectedHyperlaneId),
                replace: true);
        }
        catch (Exception ex)
        {
            hyperlaneStatus = $"Hyperlane save failed: {ex.GetBaseException().Message}";
        }
    }

    protected async Task DeleteSelectedHyperlaneAsync()
    {
        if (selectedHyperlaneId <= 0)
        {
            return;
        }

        var sector = GetSelectedSector();
        try
        {
            await SectorRouteService.DeleteSavedRouteAsync(sector.Id, selectedHyperlaneId);
            await RefreshExplorerDataAsync();

            var reloadedSector = GetSelectedSector();
            selectedSystemText = FormatSelectedSystem(reloadedSector, selectedSystemId);
            LoadHyperlaneForm();
            pendingHyperlaneStatus = "Deleted saved hyperlane.";
            NavigationManager.NavigateTo(
                SectorExplorerRoutes.BuildSectionUri("Hyperlanes", selectedSectorId, selectedSystemId),
                replace: true);
        }
        catch (Exception ex)
        {
            hyperlaneStatus = $"Hyperlane delete failed: {ex.GetBaseException().Message}";
        }
    }

    protected void LoadHyperlaneForm(SectorSavedRoute? route = null)
    {
        var orderedRoutes = GetOrderedRoutes();
        var routeToLoad = route
            ?? orderedRoutes.FirstOrDefault(item => item.Id == RequestedHyperlaneId)
            ?? orderedRoutes.FirstOrDefault(item => item.Id == selectedHyperlaneId);
        if (routeToLoad is null)
        {
            StartNewHyperlane();
            return;
        }

        selectedHyperlaneId = routeToLoad.Id;
        hyperlaneSourceSystemId = routeToLoad.SourceSystemId;
        hyperlaneTargetSystemId = routeToLoad.TargetSystemId;
        hyperlaneTechnologyLevel = routeToLoad.TechnologyLevel;
        hyperlaneTierName = routeToLoad.TierName;
        hyperlaneDistanceParsecs = Math.Round(routeToLoad.DistanceParsecs, 3);
        hyperlaneTravelTimeYears = Math.Round(routeToLoad.TravelTimeYears, 6);
        hyperlanePrimaryOwnerEmpireId = routeToLoad.PrimaryOwnerEmpireId ?? 0;
        hyperlaneSecondaryOwnerEmpireId = routeToLoad.SecondaryOwnerEmpireId ?? 0;
        hyperlaneIsUserPersisted = routeToLoad.IsUserPersisted;
        hyperlaneStatus = string.Empty;
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

        await LoadSelectedHyperlaneDataAsync(workspaceSectorId, cancellationToken);
    }

    private SectorSavedRoute? ResolveSelectedHyperlane()
    {
        if (RequestedHyperlaneId is int requestedHyperlaneId)
        {
            return SavedHyperlanes.FirstOrDefault(route => route.Id == requestedHyperlaneId);
        }

        if (selectedHyperlaneId > 0)
        {
            return SavedHyperlanes.FirstOrDefault(route => route.Id == selectedHyperlaneId);
        }

        return null;
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
        if (HasSavedHyperlanes)
        {
            LoadHyperlaneForm(ResolveSelectedHyperlane());
        }

        NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Hyperlanes", selectedSectorId, selectedSystemId), replace: true);
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
            new ExplorerSessionSelection(selectedSectorId, selectedSystemId, false, SectorExplorerRoutes.GetSectionSlug("Hyperlanes")));
    }

    private async Task LoadSelectedHyperlaneDataAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        if (sectorId <= 0)
        {
            hyperlaneSetupState = null;
            selectedHyperlaneState = null;
            ApplyHyperlanePageState(null);
            return;
        }

        hyperlaneSetupState = await ExplorerQueryService.LoadHyperlaneSetupAsync(sectorId, cancellationToken);
        if (hyperlaneSetupState is null || hyperlaneSetupState.SavedRouteCount == 0)
        {
            selectedHyperlaneState = null;
            ApplyHyperlanePageState(null);
            return;
        }

        selectedHyperlaneState = await ExplorerQueryService.LoadHyperlanePageStateAsync(sectorId, cancellationToken);
        ApplyHyperlanePageState(selectedHyperlaneState);
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

    private static (int SourceSystemId, int TargetSystemId) GetRouteKey(int sourceSystemId, int targetSystemId)
    {
        return sourceSystemId <= targetSystemId
            ? (sourceSystemId, targetSystemId)
            : (targetSystemId, sourceSystemId);
    }

    private void ApplyPendingHyperlaneStatus()
    {
        if (string.IsNullOrWhiteSpace(pendingHyperlaneStatus))
        {
            return;
        }

        hyperlaneStatus = pendingHyperlaneStatus;
        pendingHyperlaneStatus = string.Empty;
    }

    private void ClearSelectedHyperlane()
    {
        selectedHyperlaneId = 0;
        hyperlaneStatus = string.Empty;
        pendingHyperlaneStatus = string.Empty;
    }

    private void ApplyHyperlanePageState(ExplorerHyperlanePageState? state)
    {
        if (state is null)
        {
            orderedSavedRoutes = [];
            hyperlaneSystemsById = new Dictionary<int, ExplorerHyperlaneSystem>();
            hyperlaneSystemNamesById = new Dictionary<int, string>();
            return;
        }

        hyperlaneSystemsById = state.Systems.ToDictionary(system => system.SystemId);
        hyperlaneSystemNamesById = state.Systems.ToDictionary(system => system.SystemId, system => system.Name);
        orderedSavedRoutes = state.SavedRoutes
            .OrderBy(route => hyperlaneSystemNamesById.TryGetValue(route.SourceSystemId, out var sourceName) ? sourceName : string.Empty)
            .ThenBy(route => hyperlaneSystemNamesById.TryGetValue(route.TargetSystemId, out var targetName) ? targetName : string.Empty)
            .ThenBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ToList();
    }

    private string GetSystemDisplayName(int systemId)
    {
        if (hyperlaneSystemNamesById.TryGetValue(systemId, out var systemName))
        {
            return systemName;
        }

        var fallbackSystem = GetSelectedSector().Systems.FirstOrDefault(system => system.Id == systemId);
        return fallbackSystem?.Name ?? $"System {systemId}";
    }
}
