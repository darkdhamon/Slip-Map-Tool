using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;

namespace StarWin.Application.Services;

public interface IStarWinWorkspace
{
    bool IsLoaded { get; }

    IReadOnlyList<StarWinSector> Sectors { get; }

    StarWinSector CurrentSector { get; }

    IReadOnlyList<AlienRace> AlienRaces { get; }

    IReadOnlyList<Empire> Empires { get; }

    IReadOnlyList<EmpireContact> EmpireContacts { get; }

    CivilizationGeneratorSettings CivilizationSettings { get; }

    ArmyGeneratorSettings ArmySettings { get; }

    GurpsTemplate PreviewGurpsTemplate { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);
}
