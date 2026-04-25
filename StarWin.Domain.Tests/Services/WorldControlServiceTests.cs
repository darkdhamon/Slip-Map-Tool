using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class WorldControlServiceTests
{
    [Fact]
    public void TransferControl_SetsControlledStatusForFoundingEmpire()
    {
        var service = new WorldControlService();
        var world = new World
        {
            Colony = new Colony
            {
                FoundingEmpireId = 42,
                PoliticalStatus = ColonyPoliticalStatus.Independent
            }
        };
        var empire = new Empire { Id = 42 };

        service.TransferControl(world, empire);

        Assert.Equal(42, world.ControlledByEmpireId);
        Assert.Equal(42, world.Colony.ControllingEmpireId);
        Assert.Equal(ColonyPoliticalStatus.Controlled, world.Colony.PoliticalStatus);
    }

    [Fact]
    public void TransferControl_SetsSubjectStatusForNonFoundingEmpire()
    {
        var service = new WorldControlService();
        var world = new World
        {
            Colony = new Colony
            {
                FoundingEmpireId = 7,
                PoliticalStatus = ColonyPoliticalStatus.Controlled
            }
        };
        var empire = new Empire { Id = 99 };

        service.TransferControl(world, empire);

        Assert.Equal(99, world.ControlledByEmpireId);
        Assert.Equal(99, world.Colony.ControllingEmpireId);
        Assert.Equal(ColonyPoliticalStatus.Subject, world.Colony.PoliticalStatus);
    }

    [Fact]
    public void ClearControl_ClearsWorldAndColonyControl()
    {
        var service = new WorldControlService();
        var world = new World
        {
            ControlledByEmpireId = 99,
            Colony = new Colony
            {
                ControllingEmpireId = 99,
                PoliticalStatus = ColonyPoliticalStatus.Subject
            }
        };

        service.ClearControl(world);

        Assert.Null(world.ControlledByEmpireId);
        Assert.Null(world.Colony.ControllingEmpireId);
        Assert.Equal(ColonyPoliticalStatus.Independent, world.Colony.PoliticalStatus);
    }
}
