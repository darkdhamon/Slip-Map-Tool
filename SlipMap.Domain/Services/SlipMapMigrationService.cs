using SlipMap.Domain.Services.Abstract;
using SlipMapEntity = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services;

public sealed class SlipMapMigrationService : ISlipMapMigrationService
{
    private readonly ILegacySlipMapReader _legacySlipMapReader;
    private readonly ILegacySlipMapMapper _legacySlipMapMapper;

    public SlipMapMigrationService(ILegacySlipMapReader legacySlipMapReader, ILegacySlipMapMapper? legacySlipMapMapper = null)
    {
        _legacySlipMapReader = legacySlipMapReader;
        _legacySlipMapMapper = legacySlipMapMapper ?? new LegacySlipMapMapper();
    }

    public async Task<SlipMapEntity> MigrateAsync(string legacySaveFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacySaveFilePath);

        var legacyMap = await _legacySlipMapReader.ReadAsync(legacySaveFilePath, cancellationToken).ConfigureAwait(false);
        return _legacySlipMapMapper.Map(legacyMap);
    }

    public async Task<SlipMapEntity> MigrateAndSaveAsync(
        string legacySaveFilePath,
        string mapName,
        ISlipMapDataSource targetDataSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetDataSource);

        var migratedMap = await MigrateAsync(legacySaveFilePath, cancellationToken).ConfigureAwait(false);
        await targetDataSource.SaveAsync(mapName, migratedMap, cancellationToken).ConfigureAwait(false);
        return migratedMap;
    }

}
