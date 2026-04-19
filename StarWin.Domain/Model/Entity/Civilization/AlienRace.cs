namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class AlienRace
{
    public int Id { get; set; }

    public int HomePlanetId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string EnvironmentType { get; set; } = string.Empty;

    public string BodyChemistry { get; set; } = string.Empty;

    public string GovernmentType { get; set; } = string.Empty;

    public string BodyCoverType { get; set; } = string.Empty;

    public string AppearanceType { get; set; } = string.Empty;

    public string Diet { get; set; } = string.Empty;

    public string Reproduction { get; set; } = string.Empty;

    public string ReproductionMethod { get; set; } = string.Empty;

    public string Religion { get; set; } = string.Empty;

    public byte Devotion { get; set; }

    public AlienDevotionLevel DevotionLevel { get; set; }

    public AlienBiologyProfile BiologyProfile { get; set; } = new();

    public short MassKg { get; set; }

    public short SizeCm { get; set; }

    public byte LimbPairCount { get; set; }

    public IList<string> LimbTypes { get; } = new List<string>();

    public IList<string> Abilities { get; } = new List<string>();

    public IList<string> BodyCharacteristics { get; } = new List<string>();

    public IList<string> EyeCharacteristics { get; } = new List<string>();

    public IList<string> EyeColors { get; } = new List<string>();

    public IList<string> HairColors { get; } = new List<string>();

    public IList<string> Colors { get; } = new List<string>();

    public byte[] LegacyAttributes { get; set; } = Array.Empty<byte>();
}
