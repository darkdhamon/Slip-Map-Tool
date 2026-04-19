using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IColonyStatusBuilder
{
    ColonyStatus Build(Colony colony, World world);
}
