using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public interface IStarWinSpaceHabitatService
{
    Task<SpaceHabitat> CreateOrbitingAstralBodyAsync(
        int starSystemId,
        int astralBodySequence,
        int empireId,
        string? name,
        CancellationToken cancellationToken = default);

    Task<SpaceHabitat> CreateOrbitingWorldAsync(
        int worldId,
        int empireId,
        string? name,
        CancellationToken cancellationToken = default);
}
