namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2PlanetRecord
{
    public int StarSystemId { get; init; }

    public short AlienRaceId { get; init; }

    public int FirstMoonId { get; init; }

    public ushort AllegianceId { get; init; }

    public byte AtmosphereType { get; init; }

    public byte WorldType { get; init; }

    public byte WaterType { get; init; }

    public byte AtmosphereComposition { get; init; }

    public byte SmallestMolecularWeightRetained { get; init; }

    public byte AxialTiltDegrees { get; init; }

    public byte OrbitalInclinationDegrees { get; init; }

    public short AverageTemperatureCelsius { get; init; }

    public byte SatelliteCount { get; init; }

    public byte[] Hydrography { get; init; } = Array.Empty<byte>();

    public int RotationPeriodHours { get; init; }

    public int DiameterKm { get; init; }

    public byte DensityTenthsEarth { get; init; }

    public float AtmosphericPressure { get; init; }

    public ushort EccentricityThousandths { get; init; }

    public short OrbitRadiusTenthsAu { get; init; }

    public byte[] MineralResources { get; init; } = Array.Empty<byte>();

    public byte MiscellaneousFlags { get; init; }

    public byte[] UnusualFeatureFlags { get; init; } = Array.Empty<byte>();

    public string Name { get; init; } = string.Empty;
}
