using SlipMap.Domain.Exceptions;
using SlipMap.Domain.Model.ViewModel;
using SlipMap.Domain.Services;
using SlipMap.Domain.Services.Abstract;

namespace SlipMap.Domain.Model.Entity;

public sealed class SlipMap
{
    private readonly Dictionary<int, StarSystem> _systems = [];
    private readonly HashSet<SlipRoute> _routes = [];

    private SlipMap(int lastSystemId, int currentSystemId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lastSystemId);
        ValidateSystemIdInSector(currentSystemId, lastSystemId);

        LastSystemId = lastSystemId;
        CurrentSystemId = currentSystemId;
        AddSystem(currentSystemId);
    }

    public int LastSystemId { get; }

    public int TotalSystemCount => LastSystemId + 1;

    public int CurrentSystemId { get; private set; }

    public StarSystem CurrentSystem => GetSystem(CurrentSystemId);

    public IReadOnlyCollection<StarSystem> VisitedSystems => _systems.Values
        .OrderBy(system => system.Name)
        .ThenBy(system => system.Id)
        .ToList();

    public IReadOnlyCollection<SlipRoute> Routes => _routes
        .OrderBy(route => route.FirstSystemId)
        .ThenBy(route => route.SecondSystemId)
        .ToList();

    public static SlipMap Create(int lastSystemId, int? currentSystemId = null, IRandomNumberGenerator? random = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lastSystemId);

        var selectedCurrentSystemId = currentSystemId ?? (random ?? new SystemRandomNumberGenerator()).Next(0, lastSystemId + 1);
        return new SlipMap(lastSystemId, selectedCurrentSystemId);
    }

    public StarSystem AddSystem(int systemId, string? name = null, string? notes = null)
    {
        ValidateSystemIdInSector(systemId, LastSystemId);

        if (_systems.TryGetValue(systemId, out var existingSystem))
        {
            existingSystem.UpdateDetails(name, notes);
            return existingSystem;
        }

        var system = new StarSystem(systemId, name, notes);
        _systems.Add(system.Id, system);
        return system;
    }

    public StarSystem GetSystem(int systemId)
    {
        if (_systems.TryGetValue(systemId, out var system))
        {
            return system;
        }

        throw new KeyNotFoundException($"System {systemId} has not been visited or recorded on this slip map.");
    }

    public bool HasSystem(int systemId)
    {
        return _systems.ContainsKey(systemId);
    }

    public void SetCurrentSystem(int systemId, bool recordIfUnknown = true)
    {
        if (recordIfUnknown)
        {
            AddSystem(systemId);
        }
        else if (!HasSystem(systemId))
        {
            throw new KeyNotFoundException($"System {systemId} has not been visited or recorded on this slip map.");
        }

        CurrentSystemId = systemId;
    }

    public SlipRoute AddRoute(int originSystemId, int destinationSystemId)
    {
        AddSystem(originSystemId);
        AddSystem(destinationSystemId);

        var route = new SlipRoute(originSystemId, destinationSystemId);
        if (!_routes.Add(route))
        {
            throw new DuplicateSlipRouteException(route);
        }

        return route;
    }

    public bool TryAddRoute(int originSystemId, int destinationSystemId, out SlipRoute route)
    {
        AddSystem(originSystemId);
        AddSystem(destinationSystemId);

        route = new SlipRoute(originSystemId, destinationSystemId);
        return _routes.Add(route);
    }

    public IReadOnlyList<StarSystem> GetConnectedSystems(int systemId)
    {
        _ = GetSystem(systemId);

        return _routes
            .Where(route => route.Contains(systemId))
            .Select(route => GetSystem(route.GetOtherSystemId(systemId)))
            .OrderBy(system => system.Name)
            .ThenBy(system => system.Id)
            .ToList();
    }

    public RoutePlan FindRoute(int destinationSystemId)
    {
        return FindRoute(CurrentSystemId, destinationSystemId);
    }

    public RoutePlan FindRoute(int originSystemId, int destinationSystemId)
    {
        _ = GetSystem(originSystemId);
        _ = GetSystem(destinationSystemId);

        if (originSystemId == destinationSystemId)
        {
            return new RoutePlan([GetSystem(originSystemId)]);
        }

        var visited = new HashSet<int> { originSystemId };
        var pending = new Queue<RouteSearchNode>();
        pending.Enqueue(new RouteSearchNode(originSystemId, null));

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var connectedSystem in GetConnectedSystems(current.SystemId))
            {
                if (!visited.Add(connectedSystem.Id))
                {
                    continue;
                }

                var next = new RouteSearchNode(connectedSystem.Id, current);
                if (connectedSystem.Id == destinationSystemId)
                {
                    return BuildRoutePlan(next);
                }

                pending.Enqueue(next);
            }
        }

        throw new RouteNotFoundException(originSystemId, destinationSystemId);
    }

    public void Normalize()
    {
        foreach (var system in _systems.Values)
        {
            system.UpdateDetails(system.Name?.Trim(), system.Notes?.Trim());
        }
    }

    private RoutePlan BuildRoutePlan(RouteSearchNode destination)
    {
        var route = new Stack<StarSystem>();
        for (var current = destination; current is not null; current = current.Parent)
        {
            route.Push(GetSystem(current.SystemId));
        }

        return new RoutePlan(route.ToList());
    }

    private static void ValidateSystemIdInSector(int systemId, int lastSystemId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(systemId);
        if (systemId > lastSystemId)
        {
            throw new ArgumentOutOfRangeException(nameof(systemId), systemId, $"System id cannot be greater than the sector's last system id ({lastSystemId}).");
        }
    }

    private sealed record RouteSearchNode(int SystemId, RouteSearchNode? Parent);
}
