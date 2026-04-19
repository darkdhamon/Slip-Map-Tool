
namespace StarWin.Domain.Services.Abstract;

public interface ISpaceHabitatConstructionService
{
    SpaceHabitat BuildOrbitingAstralBody(Empire builder, AstralBody astralBody, string name);

    SpaceHabitat BuildOrbitingWorld(Empire builder, World world, string name);
}
