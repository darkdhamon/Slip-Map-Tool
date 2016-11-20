#region Using Statements

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

#endregion

namespace SlipMap.Model.Entities
{
   [DataContract]
   public class Campaign
   {
      private List<Sector> _trackedSectorNames;
      private List<Ship> _trackedShips;

      public List<Ship> TrackedShipNames
      {
         get { return _trackedShips ?? (_trackedShips = new List<Ship>()); }
         set { _trackedShips = value; }
      }

      public List<Sector> TrackedSectors
      {
         get { return _trackedSectorNames ?? (_trackedSectorNames = new List<Sector>()); }
         set { _trackedSectorNames = value; }
      }

      [Key]
      [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public int Id { get; set; }

      public string Name { get; set; }
   }
}