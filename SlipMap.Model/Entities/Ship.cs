using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.Model.Entities
{
   public class Ship
   {
      public string Name { get; set; }
      public MapLocation CurrentLocation { get; set; }
      public List<MapLocation> HostileSystems { get; set; } 
      public List<Pilot> Pilots { get; set; }
   }
}
