using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

[Flags]
public enum ExplorerSectorLoadSections
{
    None = 0,
    AstralBodies = 1 << 0,
    Worlds = 1 << 1,
    WorldCharacteristics = 1 << 2,
    Colonies = 1 << 3,
    ColonyDemographics = 1 << 4,
    SpaceHabitats = 1 << 5,
    SavedRoutes = 1 << 6,
    History = 1 << 7
}

public interface IStarWinExplorerContextService
{
    Task<StarWinExplorerContext> LoadShellAsync(
        bool includeSavedRoutes = true,
        bool includeReferenceData = true,
        int? detailedSectorId = null,
        ExplorerSectorLoadSections detailedSectorSections = ExplorerSectorLoadSections.None,
        CancellationToken cancellationToken = default);

    Task<StarWinExplorerContext> LoadShellAsync(
        int? preferredSectorId = null,
        bool includeSavedRoutes = false,
        bool includeReferenceData = false,
        CancellationToken cancellationToken = default)
        => LoadShellAsync(
            includeSavedRoutes,
            includeReferenceData,
            preferredSectorId,
            ExplorerSectorLoadSections.None,
            cancellationToken);
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
