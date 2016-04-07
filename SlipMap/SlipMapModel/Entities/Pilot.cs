using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class Pilot   
   {
      [Key]
      public int Id { get; set; }

      public string Name { get; set; }

      public int PilotSkill { get; set; }
   }
}