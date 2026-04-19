using SlipMapEntity = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services.Abstract;

public interface ISlipMapMigrationService
{
    Task<SlipMapEntity> MigrateAsync(string legacySaveFilePath, CancellationToken cancellationToken = default);

    Task<SlipMapEntity> MigrateAndSaveAsync(
        string legacySaveFilePath,
        string mapName,
        ISlipMapDataSource targetDataSource,
        CancellationToken cancellationToken = default);
}
