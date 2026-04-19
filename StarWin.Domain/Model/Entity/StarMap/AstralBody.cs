namespace StarWin.Domain.Model.Entity.StarMap;

public class AstralBody
{
    public int Id { get; set; }

    public AstralBodyRole Role { get; set; }

    public AstralBodyKind Kind { get; set; } = AstralBodyKind.Unknown;

    public string Classification { get; set; } = string.Empty;

    public byte ClassificationCode { get; set; }

    public byte DecimalClassCode { get; set; }

    public byte PlanetCount { get; set; }

    public double? Luminosity { get; set; }

    public double? SolarMasses { get; set; }

    public double? CompanionOrbitAu { get; set; }

    public IList<short> PlanetOrbitTenthsAu { get; } = new List<short>();
}
