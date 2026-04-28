namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class AlienRace
{
    public int Id { get; set; }

    public int HomePlanetId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string EnvironmentType { get; set; } = string.Empty;

    public string BodyChemistry { get; set; } = string.Empty;

    public string BodyCoverType { get; set; } = string.Empty;

    public string AppearanceType { get; set; } = string.Empty;

    public string Diet { get; set; } = string.Empty;

    public string Reproduction { get; set; } = string.Empty;

    public string ReproductionMethod { get; set; } = string.Empty;

    public byte Devotion { get; set; }

    public AlienDevotionLevel DevotionLevel { get; set; }

    public CivilizationProfile CivilizationProfile { get; set; } = new();

    public AlienBiologyProfile BiologyProfile { get; set; } = new();

    public string GravityPreference { get; set; } = string.Empty;

    public string TemperaturePreference { get; set; } = string.Empty;

    public string AtmosphereBreathed { get; set; } = string.Empty;

    public short MassKg { get; set; }

    public short SizeCm { get; set; }

    public byte LimbPairCount { get; set; }

    public IList<string> LimbTypes { get; set; } = new List<string>();

    public IList<string> Abilities { get; set; } = new List<string>();

    public IList<string> BodyCharacteristics { get; set; } = new List<string>();

    public IList<string> EyeCharacteristics { get; set; } = new List<string>();

    public IList<string> EyeColors { get; set; } = new List<string>();

    public IList<string> HairColors { get; set; } = new List<string>();

    public string HairType { get; set; } = string.Empty;

    public IList<string> Colors { get; set; } = new List<string>();

    public string ColorPattern { get; set; } = string.Empty;

    public byte[] LegacyAttributes { get; set; } = Array.Empty<byte>();

    public bool RequiresUserRename { get; set; }

    public string? ImportDataJson { get; set; }
}
