using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IAlienRaceStatusBuilder
{
    AlienRaceStatus Build(AlienRace alienRace);
}
