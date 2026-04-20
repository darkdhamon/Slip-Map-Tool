using StarWin.Domain.Model.Entity.Civilization;

namespace StarWin.Application.Services;

public interface IStarWinIndependentColonyService
{
    Task<IndependentColonyConversionResult> ConvertIndependentColoniesAsync(
        int sectorId,
        CancellationToken cancellationToken = default);
}

public sealed record IndependentColonyConversionResult(
    IReadOnlyList<Empire> CreatedEmpires,
    IReadOnlyList<IndependentColonyAssignment> Assignments);

public sealed record IndependentColonyAssignment(
    int ColonyId,
    int EmpireId,
    string EmpireName);
