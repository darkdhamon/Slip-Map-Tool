using DarkDhamon.Common.EntityFramework.Model;
using SlipMap.Model.Enums;

namespace SlipMap.Model.MapElements;

public class Planet:IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanetClassification Classification { get; set; }
}