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
      public StarSystem CurrentLocation { get; set; }
      public List<StarSystem> HostileSystems { get; set; } 
      public List<Pilot> Pilots { get; set; }
   }
}
