using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IEmpireStatusBuilder
{
    EmpireStatus Build(Empire empire, IEnumerable<EmpireRaceMembership> raceMemberships, IEnumerable<EmpireContact> relations);
}
