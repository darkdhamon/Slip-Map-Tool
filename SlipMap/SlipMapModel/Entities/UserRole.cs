using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class UserRole
   {
      [Key]
      public int Id { get; set; }

      public int Name { get; set; }
   }
}