namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class SectorConfiguration
{
    public int SectorId { get; set; }

    public decimal BasicHyperlaneMaximumLengthParsecs { get; set; } = 1m;

    public decimal OwnedHyperlaneBaseMaximumLengthParsecs { get; set; } = 1.2m;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
