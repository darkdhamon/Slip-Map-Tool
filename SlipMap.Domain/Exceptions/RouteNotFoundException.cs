namespace SlipMap.Domain.Exceptions;

public sealed class RouteNotFoundException : InvalidOperationException
{
    public RouteNotFoundException(int originSystemId, int destinationSystemId)
        : base($"No known slip route exists from system {originSystemId} to system {destinationSystemId}.")
    {
        OriginSystemId = originSystemId;
        DestinationSystemId = destinationSystemId;
    }

    public int OriginSystemId { get; }
    public int DestinationSystemId { get; }
}
