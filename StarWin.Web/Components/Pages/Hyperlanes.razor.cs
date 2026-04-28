using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Components.Pages;

public partial class Hyperlanes : ComponentBase
{
    private const int ExplorerListBatchSize = 120;

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
    protected ExplorerHyperlaneWorkspace? selectedWorkspace;
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

    private bool browserSessionReady;
    private bool browserSessionRestored;
    private StarWinSector? selectedSectorRecord;

    protected IReadOnlyList<StarWinSector> ExplorerSectors => explorerContext.Sectors;
    protected IReadOnlyList<ExplorerLookupOption> ExplorerEmpires => selectedWorkspace?.Empires ?? [];
    protected IReadOnlySet<int> sectorEmpireIds => GetSelectedSector().Systems.Select(system => (int)system.AllegianceId).Where(id => id > 0).ToHashSet();

    private IReadOnlyDictionary<int, string> EmpireNamesById =>
        ExplorerEmpires.ToDictionary(empire => empire.Id, empire => empire.Name);

    protected override async Task OnInitializedAsync()
    {
        await RefreshExplorerDataAsync();
        var initialSector = RequestedSectorId is int requestedSectorId
            ? ExplorerSectors.FirstOrDefault(sector => sector.Id == requestedSectorId) ?? explorerContext.CurrentSector
            : explorerContext.CurrentSector;

        selectedSectorId = initialSector.Id;
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(initialSector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(initialSector, selectedSystemId);
        LoadHyperlaneForm(initialSector, ResolveSelectedHyperlane(initialSector));
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
        }

        var sector = GetSelectedSector();
        selectedSystemId = ExplorerPageState.ResolveSelectedSystemId(sector, RequestedSystemId, selectedSystemId);
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        LoadHyperlaneForm(sector, ResolveSelectedHyperlane(sector));
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
        return selectedSectorRecord?.Id == selectedSectorId
            ? selectedSectorRecord
            : ExplorerSectors.FirstOrDefault(item => item.Id == selectedSectorId) ?? explorerContext.CurrentSector;
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
        await LoadSelectedWorkspaceAsync(selectedSectorId);
        var sector = GetSelectedSector();
        selectedSystemId = sector.Systems.FirstOrDefault()?.Id ?? 0;
        selectedSystemText = FormatSelectedSystem(sector, selectedSystemId);
        hyperlaneVisibleCount = ExplorerListBatchSize;
        LoadHyperlaneForm(sector);
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
            if (selectedHyperlaneId == 0)
            {
                StartNewHyperlane(sector);
            }

            NavigationManager.NavigateTo(SectorExplorerRoutes.BuildSectionUri("Hyperlanes", selectedSectorId, selectedSystemId), replace: true);
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

    protected IReadOnlyList<SectorSavedRoute> GetOrderedRoutes(StarWinSector sector)
    {
        var systemsById = sector.Systems.ToDictionary(system => system.Id);
        return sector.SavedRoutes
            .OrderBy(route => systemsById.TryGetValue(route.SourceSystemId, out var sourceSystem) ? sourceSystem.Name : string.Empty)
            .ThenBy(route => systemsById.TryGetValue(route.TargetSystemId, out var targetSystem) ? targetSystem.Name : string.Empty)
            .ThenBy(route => route.SourceSystemId)
            .ThenBy(route => route.TargetSystemId)
            .ToList();
    }

    protected SectorSavedRoute? GetSelectedRoute(StarWinSector sector, IReadOnlyList<SectorSavedRoute>? orderedRoutes = null)
    {
        orderedRoutes ??= GetOrderedRoutes(sector);
        return orderedRoutes.FirstOrDefault(route => route.Id == selectedHyperlaneId);
    }

    protected SectorHyperlaneNetworkReport GetSavedHyperlaneReport(StarWinSector sector)
    {
        if (selectedWorkspace is null)
        {
            return SectorHyperlaneNetworkReport.Empty;
        }

        return SectorRoutePlanner.BuildHyperlaneNetworkReport(
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

    protected void SelectHyperlane(StarWinSector sector, int routeId)
    {
        if (routeId <= 0)
        {
            return;
        }

        var route = sector.SavedRoutes.FirstOrDefault(item => item.Id == routeId);
        if (route is null)
        {
            return;
        }

        LoadHyperlaneForm(sector, route);
    }

    protected void StartNewHyperlane(StarWinSector sector)
    {
        selectedHyperlaneId = 0;
        hyperlaneStatus = string.Empty;
        hyperlaneSourceSystemId = sector.Systems.Any(system => system.Id == selectedSystemId)
            ? selectedSystemId
            : sector.Systems.FirstOrDefault()?.Id ?? 0;
        hyperlaneTargetSystemId = sector.Systems
            .Where(system => system.Id != hyperlaneSourceSystemId)
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .Select(system => system.Id)
            .FirstOrDefault();
        hyperlaneTechnologyLevel = 6;
        hyperlaneTierName = SectorRoutePlanner.GetTierName(sector.Configuration, hyperlaneTechnologyLevel);
        hyperlanePrimaryOwnerEmpireId = 0;
        hyperlaneSecondaryOwnerEmpireId = 0;
        hyperlaneIsUserPersisted = true;
        RecalculateHyperlaneTravelDefaults(sector);
    }

    protected void RecalculateHyperlaneTravelDefaults()
    {
        var sector = GetSelectedSector();
        RecalculateHyperlaneTravelDefaults(sector);
    }

    protected void RecalculateHyperlaneTravelDefaults(StarWinSector sector)
    {
        if (hyperlaneSourceSystemId <= 0 || hyperlaneTargetSystemId <= 0)
        {
            return;
        }

        var source = sector.Systems.FirstOrDefault(system => system.Id == hyperlaneSourceSystemId);
        var target = sector.Systems.FirstOrDefault(system => system.Id == hyperlaneTargetSystemId);
        if (source is null || target is null)
        {
            return;
        }

        hyperlaneTierName = SectorRoutePlanner.GetTierName(sector.Configuration, hyperlaneTechnologyLevel);
        hyperlaneDistanceParsecs = decimal.Round((decimal)SectorRoutePlanner.CalculateParsecDistance(source.Coordinates, target.Coordinates), 3);
        hyperlaneTravelTimeYears = decimal.Round((decimal)SectorRoutePlanner.CalculateHyperlaneTravelTimeYears(
            sector.Configuration,
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
            var reloadedRoute = reloadedSector.SavedRoutes.FirstOrDefault(route => route.Id == savedRoute.Id)
                ?? reloadedSector.SavedRoutes.FirstOrDefault(route =>
                    GetRouteKey(route.SourceSystemId, route.TargetSystemId)
                    == GetRouteKey(savedRoute.SourceSystemId, savedRoute.TargetSystemId));

            LoadHyperlaneForm(reloadedSector, reloadedRoute);
            hyperlaneStatus = isUpdate
                ? "Saved hyperlane changes."
                : "Created saved hyperlane.";
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
            LoadHyperlaneForm(reloadedSector);
            hyperlaneStatus = "Deleted saved hyperlane.";
        }
        catch (Exception ex)
        {
            hyperlaneStatus = $"Hyperlane delete failed: {ex.GetBaseException().Message}";
        }
    }

    protected void LoadHyperlaneForm(StarWinSector sector, SectorSavedRoute? route = null)
    {
        var orderedRoutes = GetOrderedRoutes(sector);
        var routeToLoad = route
            ?? orderedRoutes.FirstOrDefault(item => item.Id == RequestedHyperlaneId)
            ?? orderedRoutes.FirstOrDefault(item => item.Id == selectedHyperlaneId)
            ?? orderedRoutes.FirstOrDefault();
        if (routeToLoad is null)
        {
            StartNewHyperlane(sector);
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
            includeSavedRoutes: false,
            includeReferenceData: false,
            cancellationToken: cancellationToken);

        var workspaceSectorId = RequestedSectorId ?? selectedSectorId;
        if (workspaceSectorId <= 0)
        {
            workspaceSectorId = explorerContext.CurrentSector.Id;
        }

        await LoadSelectedWorkspaceAsync(workspaceSectorId, cancellationToken);
    }

    private SectorSavedRoute? ResolveSelectedHyperlane(StarWinSector sector)
    {
        if (RequestedHyperlaneId is int requestedHyperlaneId)
        {
            return sector.SavedRoutes.FirstOrDefault(route => route.Id == requestedHyperlaneId);
        }

        if (selectedHyperlaneId > 0)
        {
            return sector.SavedRoutes.FirstOrDefault(route => route.Id == selectedHyperlaneId);
        }

        return sector.SavedRoutes.FirstOrDefault();
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
        LoadHyperlaneForm(sector);
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

    private static (int SourceSystemId, int TargetSystemId) GetRouteKey(int sourceSystemId, int targetSystemId)
    {
        return sourceSystemId <= targetSystemId
            ? (sourceSystemId, targetSystemId)
            : (targetSystemId, sourceSystemId);
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
