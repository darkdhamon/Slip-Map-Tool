using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Web.Components.Explorer;

public sealed record ExplorerWorldRecord(
    StarSystem System,
    string Name,
    string Kind,
    string Type,
    double OrbitSort,
    int KindSort,
    World? World,
    SpaceHabitat? Habitat)
{
    public static ExplorerWorldRecord FromWorld(StarSystem system, World world)
    {
        var orbitSort = world.ParentWorldId is null
            ? world.OrbitRadiusAu ?? double.MaxValue
            : world.OrbitRadiusKm ?? double.MaxValue;

        return new ExplorerWorldRecord(
            system,
            world.Name,
            world.Kind.ToString(),
            world.WorldType,
            orbitSort,
            world.Kind == WorldKind.Planet ? 0 : 1,
            world,
            null);
    }

    public static ExplorerWorldRecord FromHabitat(StarSystem system, SpaceHabitat habitat)
    {
        return new ExplorerWorldRecord(
            system,
            habitat.Name,
            "SpaceHabitat",
            "Space Habitat",
            habitat.OrbitRadiusKm ?? double.MaxValue,
            2,
            null,
            habitat);
    }
}
