using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class ColonyEmpireRelationshipService : IColonyEmpireRelationshipService
{
    public ColonyEmpireRelationship GetRelationshipToEmpire(Colony colony, Empire empire)
    {
        ArgumentNullException.ThrowIfNull(colony);
        ArgumentNullException.ThrowIfNull(empire);

        return colony.GetRelationshipToEmpire(empire.Id);
    }
}
