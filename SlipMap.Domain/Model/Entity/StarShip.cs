namespace SlipMap.Domain.Model.Entity;

public sealed class StarShip
{
    private readonly HashSet<int> _visitedSystemIds = [];
    private readonly HashSet<SlipRoute> _knownRoutes = [];

    public StarShip(string name, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A ship must have a name.", nameof(name));
        }

        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string? Notes { get; private set; }

    public int? CurrentSystemId { get; private set; }

    public IReadOnlyCollection<int> VisitedSystemIds => _visitedSystemIds.Order().ToList();

    public IReadOnlyCollection<SlipRoute> KnownRoutes => _knownRoutes
        .OrderBy(route => route.FirstSystemId)
        .ThenBy(route => route.SecondSystemId)
        .ToList();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A ship must have a name.", nameof(name));
        }

        Name = name.Trim();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void MoveTo(int systemId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(systemId);

        CurrentSystemId = systemId;
        _visitedSystemIds.Add(systemId);
    }

    public bool LearnRoute(SlipRoute route)
    {
        return _knownRoutes.Add(route);
    }

    public bool KnowsRoute(int originSystemId, int destinationSystemId)
    {
        return _knownRoutes.Contains(new SlipRoute(originSystemId, destinationSystemId));
    }
}
