using SlipMap.Domain.Model.Entity;

namespace SlipMap.Domain.Model.ViewModel;

public sealed record RoutePlan(IReadOnlyList<StarSystem> Systems)
{
    public int JumpCount => Math.Max(0, Systems.Count - 1);
}
