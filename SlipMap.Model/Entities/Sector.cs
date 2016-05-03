using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SlipMap.Model.Entities
{
   public class Sector
   {
      public string Name { get; set; }
      public int LastSystemID { get; set; }
      public List<StarSystem> Systems { get; set; }
      [JsonIgnore]
      [NotMapped]
      public int NumSystems => LastSystemID + 1;
   }
}