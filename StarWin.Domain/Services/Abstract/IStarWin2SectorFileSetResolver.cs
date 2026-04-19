using StarWin.Domain.Model.Entity.Legacy;

namespace StarWin.Domain.Services.Abstract;

public interface IStarWin2SectorFileSetResolver
{
    StarWin2SectorFileSet Resolve(string sectorFilePath);
}
