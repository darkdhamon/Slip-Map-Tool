using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class Colony
   {
      [Key]
      public int Id { get; set; }
      public List<Note> GMNotes { get; set; }
      public List<Note> PlayerNotes { get; set; }
   }
}