using SlipMap.Domain.Model.Entity.Legacy;
using SlipMap.Domain.Services.Abstract;
using SlipMapEntity = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services;

public sealed class LegacySlipMapMapper : ILegacySlipMapMapper
{
    public SlipMapEntity Map(LegacySlipMap legacyMap)
    {
        ArgumentNullException.ThrowIfNull(legacyMap);

        var highestKnownSystemId = legacyMap.StarSystems
            .SelectMany(system => system.ConnectedSystemIds.Append(system.Id))
            .Concat(legacyMap.Routes.SelectMany(route => new[] { route.FirstSystemId, route.SecondSystemId }))
            .DefaultIfEmpty(legacyMap.CurrentSystemId)
            .Append(legacyMap.LastSystemId)
            .Max();

        var map = SlipMapEntity.Create(highestKnownSystemId, legacyMap.CurrentSystemId);

        foreach (var legacySystem in legacyMap.StarSystems.OrderBy(system => system.Id))
        {
            map.AddSystem(legacySystem.Id, legacySystem.Name, legacySystem.Notes);
        }

        foreach (var legacySystem in legacyMap.StarSystems)
        {
            foreach (var connectedSystemId in legacySystem.ConnectedSystemIds)
            {
                map.TryAddRoute(legacySystem.Id, connectedSystemId, out _);
            }
        }

        foreach (var legacyRoute in legacyMap.Routes)
        {
            map.TryAddRoute(legacyRoute.FirstSystemId, legacyRoute.SecondSystemId, out _);
        }

        map.SetCurrentSystem(legacyMap.CurrentSystemId);
        return map;
    }
}
