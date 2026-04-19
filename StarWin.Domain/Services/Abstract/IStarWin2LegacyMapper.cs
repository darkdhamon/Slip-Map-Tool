using StarWin.Domain.Model.Entity.Legacy;

namespace StarWin.Domain.Services.Abstract;

public interface IStarWin2LegacyMapper
{
    StarWinSector Map(StarWin2SectorFileSet fileSet, StarWin2LegacySector legacySector);
}
