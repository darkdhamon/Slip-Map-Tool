using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class Ship
   {
      [Key]
      public int Id { get; set; }

      public int Name { get; set; }

      public StarSystem CurrentStarSystem { get; set; }

      public List<Pilot> Pilots { get; set; }
   }
}