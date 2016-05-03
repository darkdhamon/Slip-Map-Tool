using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlipMap.Model.Entities
{
   public class Pilot
   {
      public string Name { get; set; }
      public int PilotSkill { get; set; }
   }
}