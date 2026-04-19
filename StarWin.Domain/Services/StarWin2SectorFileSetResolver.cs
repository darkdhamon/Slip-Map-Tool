using StarWin.Domain.Model.Entity.Legacy;
using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class StarWin2SectorFileSetResolver : IStarWin2SectorFileSetResolver
{
    public StarWin2SectorFileSet Resolve(string sectorFilePath)
    {
        if (string.IsNullOrWhiteSpace(sectorFilePath))
        {
            throw new ArgumentException("A StarWin sector file path is required.", nameof(sectorFilePath));
        }

        var fullPath = Path.GetFullPath(sectorFilePath);
        var basePath = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The sector file path must include a directory.", nameof(sectorFilePath));
        var sectorName = Path.GetFileNameWithoutExtension(fullPath);

        return new StarWin2SectorFileSet
        {
            BasePath = basePath,
            SectorName = sectorName
        };
    }
}
