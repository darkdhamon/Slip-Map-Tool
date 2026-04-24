namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class SectorConfiguration
{
    public int SectorId { get; set; }

    public decimal OffLaneMaximumDistanceParsecs { get; set; } = 2m;

    public string Tl6HyperlaneName { get; set; } = "Basic Hyperlane";

    public decimal Tl6MaximumDistanceParsecs { get; set; } = 1m;

    public decimal Tl6OffLaneSpeedMultiplier { get; set; } = 2m;

    public decimal Tl6HyperlaneSpeedModifier { get; set; } = 2m;

    public string Tl7HyperlaneName { get; set; } = "Enhanced Hyperlane";

    public decimal Tl7MaximumDistanceParsecs { get; set; } = 1.2m;

    public decimal Tl7OffLaneSpeedMultiplier { get; set; } = 4m;

    public decimal Tl7HyperlaneSpeedModifier { get; set; } = 2.25m;

    public string Tl8HyperlaneName { get; set; } = "Advanced Hyperlane";

    public decimal Tl8MaximumDistanceParsecs { get; set; } = 1.4m;

    public decimal Tl8OffLaneSpeedMultiplier { get; set; } = 8m;

    public decimal Tl8HyperlaneSpeedModifier { get; set; } = 2.5m;

    public string Tl9HyperlaneName { get; set; } = "Prime Hyperlane";

    public decimal Tl9MaximumDistanceParsecs { get; set; } = 1.6m;

    public decimal Tl9OffLaneSpeedMultiplier { get; set; } = 16m;

    public decimal Tl9HyperlaneSpeedModifier { get; set; } = 2.75m;

    public string Tl10HyperlaneName { get; set; } = "Ascendant Hyperlane";

    public decimal Tl10MaximumDistanceParsecs { get; set; } = -1m;

    public decimal Tl10OffLaneSpeedMultiplier { get; set; } = 32m;

    public decimal Tl10HyperlaneSpeedModifier { get; set; } = 3m;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
