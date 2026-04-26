using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class SpaceHabitatConstructionServiceTests
{
    [Fact]
    public void BuildOrbitingAstralBody_UsesAstralBodyTarget()
    {
        var service = new SpaceHabitatConstructionService();
        var builder = new Empire { Id = 12 };
        var astralBody = new AstralBody { Role = AstralBodyRole.Primary };

        var habitat = service.BuildOrbitingAstralBody(builder, astralBody, "Aegis Station");

        Assert.Equal("Aegis Station", habitat.Name);
        Assert.Equal(12, habitat.BuiltByEmpireId);
        Assert.Equal(12, habitat.ControlledByEmpireId);
        Assert.Equal(OrbitTargetKind.AstralBody, habitat.OrbitTargetKind);
        Assert.Equal((int)AstralBodyRole.Primary, habitat.OrbitTargetId);
    }

    [Fact]
    public void BuildOrbitingWorld_UsesWorldTarget()
    {
        var service = new SpaceHabitatConstructionService();
        var builder = new Empire { Id = 77 };
        var world = new World { Id = 3401 };

        var habitat = service.BuildOrbitingWorld(builder, world, "Kepler Ring");

        Assert.Equal("Kepler Ring", habitat.Name);
        Assert.Equal(77, habitat.BuiltByEmpireId);
        Assert.Equal(77, habitat.ControlledByEmpireId);
        Assert.Equal(OrbitTargetKind.World, habitat.OrbitTargetKind);
        Assert.Equal(3401, habitat.OrbitTargetId);
    }
}
