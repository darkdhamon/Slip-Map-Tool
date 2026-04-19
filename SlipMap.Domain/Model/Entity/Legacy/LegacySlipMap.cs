namespace SlipMap.Domain.Model.Entity.Legacy;

public sealed class LegacySlipMap
{
    public int SchemaVersion { get; init; }

    public string? SectorFileName { get; init; }

    public int LastSystemId { get; init; }

    public int CurrentSystemId { get; init; }

    public LegacySession? LegacySession { get; init; }

    public IReadOnlyCollection<LegacyStarSystem> StarSystems { get; init; } = [];

    public IReadOnlyCollection<LegacySlipRoute> Routes { get; init; } = [];
}
