using SlipMap.Domain.Model.Entity.Legacy;
using SlipMapEntity = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services.Abstract;

public interface ILegacySlipMapMapper
{
    SlipMapEntity Map(LegacySlipMap legacyMap);
}
