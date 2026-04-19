using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class WorldControlService : IWorldControlService
{
    public void TransferControl(World world, Empire controllingEmpire)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(controllingEmpire);

        world.ControlledByEmpireId = controllingEmpire.Id;
        if (world.Colony != null)
        {
            world.Colony.ControllingEmpireId = controllingEmpire.Id;
            world.Colony.PoliticalStatus = world.Colony.IsFoundedBy(controllingEmpire.Id)
                ? ColonyPoliticalStatus.Controlled
                : ColonyPoliticalStatus.Subject;
        }
    }

    public void ClearControl(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        world.ControlledByEmpireId = null;
        if (world.Colony != null)
        {
            world.Colony.ControllingEmpireId = null;
            world.Colony.PoliticalStatus = ColonyPoliticalStatus.Independent;
        }
    }
}
