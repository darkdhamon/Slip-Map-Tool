namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class StarWinSector
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public SectorConfiguration Configuration { get; set; } = new();

    public IList<StarSystem> Systems { get; } = new List<StarSystem>();

    public IList<SectorSavedRoute> SavedRoutes { get; } = new List<SectorSavedRoute>();

    public IList<HistoryEvent> History { get; } = new List<HistoryEvent>();
}
