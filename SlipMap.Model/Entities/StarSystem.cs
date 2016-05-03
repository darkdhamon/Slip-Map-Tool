using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace SlipMap.Model.Entities
{
   public class StarSystem
   {
      private List<int> _connectedSystemIds;
      private List<StarSystem> _connectedSystems;
      
      public int StarWinId { get; set; }
      public string Name { get; set; }
      public string GMNotes { get; set; }
      public string StarwinNotes { get; set; }

      [NotMapped]
      public List<int> ConnectedSystemIds
      {
         get { return _connectedSystemIds??(_connectedSystemIds=ConnectedSystems.Select(s=>s.StarWinId).ToList()); }
         set { _connectedSystemIds = value; }
      }

      [JsonIgnore]
      public List<StarSystem> ConnectedSystems
      {
         get { return _connectedSystems??(_connectedSystems = new List<StarSystem>()); }
         set { _connectedSystems = value; }
      }
   }
}