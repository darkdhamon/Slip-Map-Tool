namespace StarWin.Domain.Model.Entity.StarMap;

public class World
{
    public int Id { get; set; }

    public WorldKind Kind { get; set; }

    public int? StarSystemId { get; set; }

    public int? ParentWorldId { get; set; }

    public int? PrimaryAstralBodySequence { get; set; }

    public string Name { get; set; } = string.Empty;

    public string WorldType { get; set; } = string.Empty;

    public string AtmosphereType { get; set; } = string.Empty;

    public string AtmosphereComposition { get; set; } = string.Empty;

    public string WaterType { get; set; } = string.Empty;

    public Hydrography Hydrography { get; set; } = new();

    public MineralResources MineralResources { get; set; } = new();

    public int DiameterKm { get; set; }

    public byte DensityTenthsEarth { get; set; }

    public float AtmosphericPressure { get; set; }

    public short AverageTemperatureCelsius { get; set; }

    public byte MiscellaneousFlags { get; set; }

    public IList<UnusualCharacteristic> UnusualCharacteristics { get; } = new List<UnusualCharacteristic>();

    public double? Albedo { get; set; }

    public int? AlienRaceId { get; set; }

    public int? ControlledByEmpireId { get; set; }

    public ushort AllegianceId { get; set; } = ushort.MaxValue;

    public double? OrbitRadiusAu { get; set; }

    public double? OrbitRadiusKm { get; set; }

    public byte? SmallestMolecularWeightRetained { get; set; }

    public byte? AxialTiltDegrees { get; set; }

    public byte? OrbitalInclinationDegrees { get; set; }

    public uint? RotationPeriodHours { get; set; }

    public ushort? EccentricityThousandths { get; set; }

    public double? OrbitPeriodDays { get; set; }

    public double? GravityEarthG { get; set; }

    public double? MassEarthMasses { get; set; }

    public double? EscapeVelocityKmPerSecond { get; set; }

    public double? OxygenPressureAtmospheres { get; set; }

    public int? BoilingPointCelsius { get; set; }

    public double? MagneticFieldGauss { get; set; }

    public Colony? Colony { get; set; }
}
