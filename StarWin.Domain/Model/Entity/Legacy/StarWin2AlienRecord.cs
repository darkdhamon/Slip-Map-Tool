namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2AlienRecord
{
    public int HomePlanetId { get; init; }

    public byte EnvironmentType { get; init; }

    public byte BodyType { get; init; }

    public byte LimbPairCount { get; init; }

    public byte Diet { get; init; }

    public byte Reproduction { get; init; }

    public byte ReproductionMethod { get; init; }

    public byte GovernmentType { get; init; }

    public byte BodyCoverType { get; init; }

    public byte AppearanceType { get; init; }

    public short MassKg { get; init; }

    public short SizeCm { get; init; }

    public byte[] LimbTypes { get; init; } = Array.Empty<byte>();

    public byte[] Attributes { get; init; } = Array.Empty<byte>();

    public byte[] AbilityFlags { get; init; } = Array.Empty<byte>();

    public byte[] ColorFlags { get; init; } = Array.Empty<byte>();

    public byte[] HairColorFlags { get; init; } = Array.Empty<byte>();

    public byte[] BodyCharacteristicFlags { get; init; } = Array.Empty<byte>();

    public byte[] EyeColorFlags { get; init; } = Array.Empty<byte>();

    public byte[] EyeCharacteristicFlags { get; init; } = Array.Empty<byte>();

    public byte HairType { get; init; }

    public byte Religion { get; init; }

    public byte Devotion { get; init; }

    public string Name { get; init; } = string.Empty;
}
