using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinExplorerContextService
{
    Task<StarWinExplorerContext> LoadShellAsync(
        int? preferredSectorId = null,
        bool includeReferenceData = false,
        CancellationToken cancellationToken = default);
}

public sealed record StarWinExplorerContext(
    IReadOnlyList<StarWinSector> Sectors,
    StarWinSector CurrentSector,
    IReadOnlyList<AlienRace> AlienRaces,
    IReadOnlyList<Empire> Empires)
{
    public static StarWinExplorerContext Empty { get; } = new(
        [],
        new StarWinSector { Id = 0, Name = "No sectors loaded" },
        [],
        []);
}
