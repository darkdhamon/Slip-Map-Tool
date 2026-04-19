namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2StarRecord
{
    public byte[] SpectralClassCodes { get; init; } = Array.Empty<byte>();

    public byte[] SpectralDecimalCodes { get; init; } = Array.Empty<byte>();

    public byte[] PlanetCounts { get; init; } = Array.Empty<byte>();

    public float[] Luminosities { get; init; } = Array.Empty<float>();

    public float[] StarMasses { get; init; } = Array.Empty<float>();

    public byte AgeTenthsBillionYears { get; init; }

    public short[] CompanionDistancesTenthsAu { get; init; } = Array.Empty<short>();

    public short[,] AstralOrbitTenthsAu { get; init; } = new short[18, 3];

    public byte StarCount { get; init; }

    public int[] FirstPlanetIds { get; init; } = Array.Empty<int>();

    public CoordinatesRecord Coordinates { get; init; }

    public ushort AllegianceId { get; init; }

    public byte Code { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte MiscellaneousFlags { get; init; }
}
