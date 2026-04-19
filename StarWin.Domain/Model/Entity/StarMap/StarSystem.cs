namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class StarSystem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Coordinates Coordinates { get; set; }

    public ushort AllegianceId { get; set; } = ushort.MaxValue;

    public byte MapCode { get; set; }

    public IList<AstralBody> AstralBodies { get; } = new List<AstralBody>();

    public IEnumerable<AstralBody> Stars => AstralBodies.Where(body => body.Kind == AstralBodyKind.Star);

    public IList<World> Worlds { get; } = new List<World>();

    public IEnumerable<World> Planets => Worlds.Where(world => world.Kind == WorldKind.Planet);

    public IEnumerable<World> Moons => Worlds.Where(world => world.Kind == WorldKind.Moon);

    public IList<SpaceHabitat> SpaceHabitats { get; } = new List<SpaceHabitat>();
}
