namespace StarWin.Application.Services;

public sealed class StarWinSearchResult
{
    public StarWinSearchResultType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string Tab { get; init; } = "Systems";

    public int? SectorId { get; init; }

    public int? SystemId { get; init; }

    public int? WorldId { get; init; }

    public int? SpaceHabitatId { get; init; }

    public int? RaceId { get; init; }

    public int? EmpireId { get; init; }
}
