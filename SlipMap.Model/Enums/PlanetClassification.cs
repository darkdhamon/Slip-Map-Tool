using System.ComponentModel;

namespace SlipMap.Model.Enums;

public enum PlanetClassification
{
    Unknown,
    Arid,
    [Description("Brown Dwarf")]
    BrownDwarf,
    [Description("Carbonnaceous Asteroid Belt")]
    CarbonaceousAsteroidBelt,
    Chunk,
    Desert,
    FailedCore,
    GasGiant,
    Glacier,
    IceBall,
    IcyAsteroidBelt,
    Jungle,
    NickelIronAsteroidBelt,
    Ocean,
    PostGarden,
    PreGarden,
    Ring,
    Rock,
    Steppe,
    StonyAsteroidBelt,
    Terrestrial,
    Tundra
}