using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class Sector
   {
      [Key]
      public int Id { get; set; }

      [StringLength(100)]
      public string Name { get; set; }

      public List<StarSystem> StarSystems { get; set; }
   }
}