using System.ComponentModel.DataAnnotations;

namespace SlipMapModel.Entities
{
   public class Note
   {
      [Key]
      public int Id { get; set; }

      [Timestamp]
      public byte[] TimeStamp { get; set; }

      public string Text { get; set; }

      public User Author { get; set; }
   }
}