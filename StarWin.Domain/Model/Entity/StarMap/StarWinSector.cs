namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class StarWinSector
{
    public string Name { get; set; } = string.Empty;

    public IList<StarSystem> Systems { get; } = new List<StarSystem>();

    public IList<AlienRace> AlienRaces { get; } = new List<AlienRace>();

    public IList<Empire> Empires { get; } = new List<Empire>();

    public IList<EmpireContact> EmpireContacts { get; } = new List<EmpireContact>();

    public IList<HistoryEvent> History { get; } = new List<HistoryEvent>();
}
