using Microsoft.AspNetCore.Components;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Web.Components.Layout;

public sealed record SectorExplorerLayoutState(
    bool ShowHero,
    IReadOnlyList<StarWinSector> Sectors,
    IReadOnlyList<StarSystem> Systems,
    int SelectedSectorId,
    EventCallback<int> SelectedSectorChanged,
    string SelectedSystemText,
    EventCallback<string> SelectedSystemTextChanged,
    string SearchQuery,
    EventCallback<string> SearchQueryChanged,
    IReadOnlyList<StarWinSearchResult> SearchResults,
    EventCallback<StarWinSearchResult> SearchResultSelected,
    IReadOnlyList<string> Sections,
    string ActiveSection,
    string ErrorMessage,
    Func<StarWinSearchResultType, string>? ResultTypeDisplayFactory,
    Func<string, string>? SectionHrefFactory,
    RenderFragment? HeaderContent = null);
