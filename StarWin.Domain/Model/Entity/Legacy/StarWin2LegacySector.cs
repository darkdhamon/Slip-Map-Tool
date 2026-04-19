namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2LegacySector
{
    public IList<StarWin2StarRecord> Stars { get; } = new List<StarWin2StarRecord>();

    public IList<StarWin2PlanetRecord> Planets { get; } = new List<StarWin2PlanetRecord>();

    public IList<StarWin2MoonRecord> Moons { get; } = new List<StarWin2MoonRecord>();

    public IList<StarWin2AlienRecord> Aliens { get; } = new List<StarWin2AlienRecord>();

    public IList<StarWin2ColonyRecord> Colonies { get; } = new List<StarWin2ColonyRecord>();

    public IList<StarWin2EmpireRecord> Empires { get; } = new List<StarWin2EmpireRecord>();

    public IList<StarWin2ContactRecord> Contacts { get; } = new List<StarWin2ContactRecord>();
}
