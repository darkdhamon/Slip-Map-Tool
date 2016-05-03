using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlipMap.Model.Entities
{
   public class Campaign
   {
      private List<string> _trackedShips;
      private List<string> _trackedSectorNames;
      
      public List<string> TrackedShipNames
      {
         get { return _trackedShips??(_trackedShips= new List<string>()); }
         set { _trackedShips = value; }
      }

      public List<string> TrackedSectorNames
      {
         get { return _trackedSectorNames??(_trackedSectorNames = new List<string>()); }
         set { _trackedSectorNames = value; }
      }

      public string Name { get; set; }
   }
}