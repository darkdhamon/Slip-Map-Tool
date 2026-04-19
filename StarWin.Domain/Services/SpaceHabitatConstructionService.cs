using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class SpaceHabitatConstructionService : ISpaceHabitatConstructionService
{
    public SpaceHabitat BuildOrbitingAstralBody(Empire builder, AstralBody astralBody, string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(astralBody);

        return new SpaceHabitat
        {
            Name = name,
            BuiltByEmpireId = builder.Id,
            ControlledByEmpireId = builder.Id,
            OrbitTargetKind = OrbitTargetKind.AstralBody,
            OrbitTargetId = (int)astralBody.Role
        };
    }

    public SpaceHabitat BuildOrbitingWorld(Empire builder, World world, string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(world);

        return new SpaceHabitat
        {
            Name = name,
            BuiltByEmpireId = builder.Id,
            ControlledByEmpireId = builder.Id,
            OrbitTargetKind = OrbitTargetKind.World,
            OrbitTargetId = world.Id
        };
    }
}
