
namespace StarWin.Domain.Services.Abstract;

public interface IIndependentColonyEmpireFactory
{
    Empire CreateEmpireFromIndependentColony(Colony colony, World world, AlienRace foundingRace, Empire? parentEmpire);
}
