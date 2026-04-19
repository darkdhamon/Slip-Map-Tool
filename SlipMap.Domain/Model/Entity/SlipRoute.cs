namespace SlipMap.Domain.Model.Entity;

public readonly record struct SlipRoute
{
    public SlipRoute(int originSystemId, int destinationSystemId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(originSystemId);
        ArgumentOutOfRangeException.ThrowIfNegative(destinationSystemId);

        if (originSystemId == destinationSystemId)
        {
            throw new ArgumentException("A slip route must connect two different systems.", nameof(destinationSystemId));
        }

        FirstSystemId = Math.Min(originSystemId, destinationSystemId);
        SecondSystemId = Math.Max(originSystemId, destinationSystemId);
    }

    public int FirstSystemId { get; }
    public int SecondSystemId { get; }

    public bool Contains(int systemId)
    {
        return FirstSystemId == systemId || SecondSystemId == systemId;
    }

    public int GetOtherSystemId(int systemId)
    {
        if (FirstSystemId == systemId)
        {
            return SecondSystemId;
        }

        if (SecondSystemId == systemId)
        {
            return FirstSystemId;
        }

        throw new ArgumentException($"System {systemId} is not part of this route.", nameof(systemId));
    }

    public override string ToString()
    {
        return $"{FirstSystemId} <-> {SecondSystemId}";
    }
}
