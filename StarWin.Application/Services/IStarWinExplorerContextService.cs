using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinExplorerContextService
{
    Task<StarWinExplorerContext> LoadShellAsync(CancellationToken cancellationToken = default);
    Task<StarWinSector?> LoadSectorAsync(int sectorId, bool includeHistory = false, CancellationToken cancellationToken = default);
}

public sealed record StarWinExplorerContext(
    IReadOnlyList<StarWinSector> Sectors,
    StarWinSector CurrentSector,
    IReadOnlyList<AlienRace> AlienRaces,
    IReadOnlyList<Empire> Empires,
    IReadOnlyList<EmpireContact> EmpireContacts)
{
    public static StarWinExplorerContext Empty { get; } = new(
        [],
        new StarWinSector { Id = 0, Name = "No sectors loaded" },
        [],
        [],
        []);
}
