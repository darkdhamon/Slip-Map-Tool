using StarWin.Domain.Model.Entity.Civilization;

namespace StarWin.Application.Services;

public interface IStarWinIndependentColonyService
{
    Task<IReadOnlyList<Empire>> ConvertIndependentColoniesAsync(
        int sectorId,
        CancellationToken cancellationToken = default);
}
