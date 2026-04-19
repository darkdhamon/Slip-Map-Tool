namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class Planet : World
{
    public Planet()
    {
        Kind = WorldKind.Planet;
    }

    public byte UnusualFeatureFlags1 { get; set; }

    public byte UnusualFeatureFlags2 { get; set; }

    public byte UnusualFeatureFlags3 { get; set; }

    public byte UnusualFeatureFlags4 { get; set; }

    public byte UnusualFeatureFlags5 { get; set; }

    public IList<World> Moons { get; } = new List<World>();
}
