using SlipMap.Domain.Model.Entity.Legacy;

namespace SlipMap.Domain.Services.Abstract;

public interface ILegacySlipMapReader
{
    Task<LegacySlipMap> ReadAsync(string saveFilePath, CancellationToken cancellationToken = default);
}
