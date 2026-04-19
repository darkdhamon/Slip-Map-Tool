using StarWin.Domain.Model.Entity.Legacy;

namespace StarWin.Domain.Services.Abstract;

public interface IStarWin2SectorImporter
{
    Task<StarWinSector> ImportAsync(StarWin2SectorFileSet fileSet, CancellationToken cancellationToken = default);
}
