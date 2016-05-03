using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlipMap.Model.Entities;

namespace SlipMap.Domain.DataAccess
{
   public class Session
   {
      public string Campaign { get; set; }
      public string Ship { get; set; }
      public string Sector { get; set; }
      public string Pilot { get; set; }
   }
}