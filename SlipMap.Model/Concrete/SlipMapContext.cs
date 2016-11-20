using System.Data.Entity;
using SlipMap.Model.Entities;

namespace SlipMap.Model.Concrete
{
   public class SlipMapContext:DbContext
   {
      public DbSet<Campaign> Campaigns { get; set; }
      public DbSet<Colony> Colonies { get; set; }
      public DbSet<Pilot> Pilots { get; set; }
      public DbSet<Sector> Sectors { get; set; }
      public DbSet<Ship> Ships { get; set; }
      public DbSet<StarSystem> StarSystems { get; set; }
   }
}
