using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class StarSystem
   {
      [Key]
      public int Id { get; set; }

      [StringLength(100)]
      public string Name { get; set; }

      public StarWinData StarWinData { get; set; }

      public List<Note> GmNotes { get; set; }

      public List<Note> PlayerNotes { get; set; }

      public List<Colony> Colonies { get; set; }

      public List<StarSystem> ConnectedSystems { get; set; }
   }
}