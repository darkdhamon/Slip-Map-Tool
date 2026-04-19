using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;

namespace StarWin.Application.Services;

public interface IStarWinWorkspace
{
    IReadOnlyList<StarWinSector> Sectors { get; }

    StarWinSector CurrentSector { get; }

    IReadOnlyList<AlienRace> AlienRaces { get; }

    IReadOnlyList<Empire> Empires { get; }

    IReadOnlyList<EmpireContact> EmpireContacts { get; }

    CivilizationGeneratorSettings CivilizationSettings { get; }

    ArmyGeneratorSettings ArmySettings { get; }

    GurpsTemplate PreviewGurpsTemplate { get; }
}
