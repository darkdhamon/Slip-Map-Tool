using SlipMap.Domain.Model.Entity;

namespace SlipMap.Domain.Exceptions;

public sealed class DuplicateSlipRouteException : InvalidOperationException
{
    public DuplicateSlipRouteException(SlipRoute route)
        : base($"Slip route already exists between systems {route.FirstSystemId} and {route.SecondSystemId}.")
    {
        Route = route;
    }

    public SlipRoute Route { get; }
}
