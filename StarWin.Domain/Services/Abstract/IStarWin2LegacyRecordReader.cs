using StarWin.Domain.Model.Entity.Legacy;

namespace StarWin.Domain.Services.Abstract;

public interface IStarWin2LegacyRecordReader
{
    Task<IReadOnlyList<StarWin2StarRecord>> ReadStarsAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StarWin2PlanetRecord>> ReadPlanetsAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StarWin2MoonRecord>> ReadMoonsAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StarWin2AlienRecord>> ReadAliensAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StarWin2ColonyRecord>> ReadColoniesAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StarWin2EmpireRecord>> ReadEmpiresAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StarWin2ContactRecord>> ReadContactsAsync(string filePath, CancellationToken cancellationToken = default);
}
