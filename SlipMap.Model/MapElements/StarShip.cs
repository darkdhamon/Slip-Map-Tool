using DarkDhamon.Common.EntityFramework.Model;

namespace SlipMap.Model.MapElements;

public class StarShip:IEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public StarSystem? CurrentStarSystem { get; set; }

    public List<StarSystem> VisitedStarSystems { get; set; } = new();

    public List<SlipRoute> KnownSlipRoutes { get; set; } = new();
    public int Id { get; set; }
}