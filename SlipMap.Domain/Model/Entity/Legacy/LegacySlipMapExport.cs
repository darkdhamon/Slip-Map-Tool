namespace SlipMap.Domain.Model.Entity.Legacy;

public sealed class LegacySlipMapExport
{
    public int SchemaVersion { get; init; }

    public DateTime ExportedAt { get; init; }

    public LegacySession? LegacySession { get; init; }

    public IReadOnlyCollection<LegacySlipMap> Maps { get; init; } = [];
}
