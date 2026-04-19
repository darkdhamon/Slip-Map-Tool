using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IWorldPhysicalProfileCalculator
{
    WorldPhysicalProfile Calculate(World world, StarSystem system, AstralBody primaryAstralBody);

    WorldPhysicalProfile Calculate(World moon, World parentWorld);
}
