namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2SectorFileSet
{
    public required string BasePath { get; init; }

    public required string SectorName { get; init; }

    public string StarSystemFilePath => GetPath(".sun");

    public string PlanetFilePath => GetPath(".pln");

    public string SatelliteFilePath => GetPath(".mon");

    public string AlienFilePath => GetPath(".aln");

    public string ColonyFilePath => GetPath(".col");

    public string EmpireFilePath => GetPath(".emp");

    public string HistoryFilePath => GetPath(".his");

    public string NameFilePath => GetPath(".nam");

    private string GetPath(string extension)
    {
        return Path.Combine(BasePath, $"{SectorName}{extension}");
    }
}
