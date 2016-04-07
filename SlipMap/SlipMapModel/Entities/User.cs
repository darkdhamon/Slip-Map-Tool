using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SlipMapModel.Entities
{
   public class User
   {
      [Key]
      public int Id { get; set; }

      public List<UserRole> UserRoles { get; set; }

      public DateTime LastLogin { get; set; }

      public DateTime Created { get; set; }

      [NotMapped]
      public UserRole LoggedInAs { get; set; }
   }
}