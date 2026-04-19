
namespace StarWin.Domain.Services.Abstract;

public interface IColonyEmpireRelationshipService
{
    ColonyEmpireRelationship GetRelationshipToEmpire(Colony colony, Empire empire);
}
