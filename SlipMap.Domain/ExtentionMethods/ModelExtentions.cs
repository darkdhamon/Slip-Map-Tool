using System;
using System.Collections.Generic;
using SlipMap.Domain.DataAccess;
using SlipMap.Domain.ShortTermModel;
using SlipMap.Model.Entities;
using Enumerable = System.Linq.Enumerable;

namespace SlipMap.Domain.ExtentionMethods
{
   public static class ModelExtentions
   {
      public static StarSystem System(this Sector sector, int id)
      {
         return Enumerable.FirstOrDefault(sector.Systems, s => s.StarWinId == id);
      }

      public static Pilot Pilot(this Ship ship, string name)
      {
         return Enumerable.FirstOrDefault(ship.Pilots, p =>
            string.Equals(
               p.Name,
               name,
               StringComparison.CurrentCultureIgnoreCase));
      }

      public static void FillConnectedStarSystems(this Sector sector)
      {
         foreach (var starSystem in sector.Systems)
         {
            starSystem.ConnectedSystems = Enumerable.ToList(Enumerable.Where(sector
               .Systems, s => Enumerable.Any(starSystem
                  .ConnectedSystemIds, sys => sys == s.StarWinId)));
         }
      }

      public static List<StarSystem> FindShortestRoute(this Sector sector, Ship ship, StarSystem dest,
         bool avoidHostile = false)
      {
         var orig = sector.System(ship.CurrentLocation.CurrentSystem.StarWinId);
         if (orig.StarWinId == dest.StarWinId) return null;
         var ignoreSystems = new List<int>();
         if (avoidHostile)
         {
            ignoreSystems.AddRange(
               Enumerable.ToList(Enumerable.Select(Enumerable.ToList(Enumerable.Where(ship
                  .HostileSystems, hs => hs.CurrentSector.Name == sector.Name)), hs => hs.CurrentSystem.StarWinId)));
         }
         var possibleRoutes = new Queue<RouteNode>();
         possibleRoutes.Enqueue(new RouteNode {StarSystem = orig});
         RouteNode current = null;
         while (possibleRoutes.Count > 0)
         {
            current = possibleRoutes.Dequeue();
            ignoreSystems.Add(current.StarSystem.StarWinId);
            if (current.StarSystem.StarWinId == dest.StarWinId)
            {
               break;
            }
            foreach (
               var connectedSystem in
                  Enumerable.Where(current.StarSystem.ConnectedSystems, system => !ignoreSystems.Contains(system.StarWinId)))
            {
               possibleRoutes.Enqueue(
                  new RouteNode
                  {
                     StarSystem = connectedSystem,
                     ParentNode = current
                  });
            }
         }
         var route = new List<StarSystem>();
         while (current != null && current.StarSystem != orig)
         {
            route.Add(current.StarSystem);
            current = current.ParentNode;
         }
         if (Enumerable.Any(route, system => system.StarWinId == dest.StarWinId))
            return route;
         throw new Exception(
            $"One does not simply walk to {Enumerable.First(sector.Systems, system => system.StarWinId == dest.StarWinId)}. No viable route exist to reach your destination... Blind Jump?");
      }

      public static void Save(this Ship ship)
      {
         LocalFiles.Save(ship);
      }

      public static void Save(this Sector sector)
      {
         LocalFiles.Save(sector);
      }

      public static void Save(this Campaign campaign)
      {
         LocalFiles.Save(campaign);
      }

      public static void Save(this Session session)
      {
         LocalFiles.Save(session);
      }
   }
}