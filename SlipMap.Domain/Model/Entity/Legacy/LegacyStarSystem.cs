namespace SlipMap.Domain.Model.Entity.Legacy;

public sealed class LegacyStarSystem
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyCollection<int> ConnectedSystemIds { get; init; } = [];
}
