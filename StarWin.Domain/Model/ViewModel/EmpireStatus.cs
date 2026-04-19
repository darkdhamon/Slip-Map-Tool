
namespace StarWin.Domain.Model.ViewModel;

public sealed class EmpireStatus
{
    public required Empire Empire { get; init; }

    public IList<EmpireRaceMembership> RaceMemberships { get; } = new List<EmpireRaceMembership>();

    public IList<EmpireContact> Relations { get; } = new List<EmpireContact>();
}
