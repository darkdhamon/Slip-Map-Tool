namespace SlipMap.Domain.Model.Entity.Legacy;

public sealed class LegacySession
{
    public string? DisplayName { get; init; }

    public int PilotSkill { get; init; }

    public string? SectorFileName { get; init; }

    public int? DestinationSystemId { get; init; }
}
