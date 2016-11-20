using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.Abstract;
using SlipMap.Model.Entities;

namespace SlipMap.Model.Concrete
{
   class SlipMapRepository : ISlipMapRepository
   {
      public SlipMapRepository()
      {
         Context = new SlipMapContext();
      }

      public SlipMapRepository(SlipMapContext context)
      {
         Context = context;
      }

      private SlipMapContext Context { get; }
      public IQueryable<Ship> Ships => Context.Ships;
      public IQueryable<Campaign> Campaigns => Context.Campaigns;
      public IQueryable<Sector> Sectors => Context.Sectors;
      public IQueryable<Pilot> Pilots => Context.Pilots;
      public IQueryable<StarSystem> StarSystems => Context.StarSystems;
      public IQueryable<Colony> Colonies => Context.Colonies;
   }
}
