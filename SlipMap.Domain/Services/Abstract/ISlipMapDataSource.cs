using SlipMapEntity = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services.Abstract;

public interface ISlipMapDataSource
{
    Task SaveAsync(string mapName, SlipMapEntity map, CancellationToken cancellationToken = default);

    Task<SlipMapEntity> LoadAsync(string mapName, CancellationToken cancellationToken = default);

    Task ExportAsync(string filePath, SlipMapEntity map, CancellationToken cancellationToken = default);

    Task<SlipMapEntity> ImportAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SlipMapEntity>> ImportAllAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default);
}
