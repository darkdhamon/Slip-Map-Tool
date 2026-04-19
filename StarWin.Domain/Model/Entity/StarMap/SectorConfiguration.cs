namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class SectorConfiguration
{
    public int SectorId { get; set; }

    public string BasicHyperlaneTierName { get; set; } = "Basic";

    public decimal BasicHyperlaneMaximumLengthParsecs { get; set; } = 1m;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
