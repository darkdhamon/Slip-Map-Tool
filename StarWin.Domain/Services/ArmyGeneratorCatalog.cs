
namespace StarWin.Domain.Services;

public static class ArmyGeneratorCatalog
{
    public static readonly IReadOnlyDictionary<string, StarshipClass> StarshipCodes =
        new Dictionary<string, StarshipClass>(StringComparer.OrdinalIgnoreCase)
        {
            ["MT"] = StarshipClass.Monitor,
            ["SD"] = StarshipClass.SuperDreadnought,
            ["CVA"] = StarshipClass.CapitalCarrier,
            ["FTA"] = StarshipClass.CapitalFreighter,
            ["BB"] = StarshipClass.Battleship,
            ["BC"] = StarshipClass.BattleCruiser,
            ["CV"] = StarshipClass.Carrier,
            ["CA"] = StarshipClass.HeavyCruiser,
            ["FT"] = StarshipClass.Freighter,
            ["AS"] = StarshipClass.AssaultShip,
            ["FG"] = StarshipClass.Frigate,
            ["DD"] = StarshipClass.Destroyer,
            ["CL"] = StarshipClass.LightCruiser,
            ["CVL"] = StarshipClass.LightCarrier,
            ["CVE"] = StarshipClass.EscortShipCarrier,
            ["FTL"] = StarshipClass.LightFreighter,
            ["ASL"] = StarshipClass.LightAssaultShip,
            ["ES"] = StarshipClass.EscortShip,
            ["CT"] = StarshipClass.Corvette
        };

    public static readonly IReadOnlyDictionary<string, MilitaryInstallationType> InstallationCodes =
        new Dictionary<string, MilitaryInstallationType>(StringComparer.OrdinalIgnoreCase)
        {
            ["PDC"] = MilitaryInstallationType.ArmoredGroundBase,
            ["SB"] = MilitaryInstallationType.Starbase,
            ["BS"] = MilitaryInstallationType.Battlestation
        };
}
