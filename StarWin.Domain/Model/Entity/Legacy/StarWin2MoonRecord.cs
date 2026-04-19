namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2MoonRecord
{
    public int PlanetId { get; init; }

    public byte AtmosphereType { get; init; }

    public byte WorldType { get; init; }

    public byte WaterType { get; init; }

    public byte AtmosphereComposition { get; init; }

    public short AverageTemperatureCelsius { get; init; }

    public byte[] Hydrography { get; init; } = Array.Empty<byte>();

    public int DiameterKm { get; init; }

    public byte DensityTenthsEarth { get; init; }

    public float AtmosphericPressure { get; init; }

    public ushort OrbitRadiusTenthsAu { get; init; }

    public byte[] MineralResources { get; init; } = Array.Empty<byte>();

    public byte MiscellaneousFlags { get; init; }

    public string Name { get; init; } = string.Empty;
}
