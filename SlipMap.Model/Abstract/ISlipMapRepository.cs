using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.Concrete;
using SlipMap.Model.Entities;

namespace SlipMap.Model.Abstract
{
   public interface ISlipMapRepository
   {
      IQueryable<Ship> Ships { get; }
      IQueryable<Campaign> Campaigns { get; }
      IQueryable<Sector> Sectors { get; }
      IQueryable<Pilot> Pilots { get; }
      IQueryable<StarSystem> StarSystems { get; }
      IQueryable<Colony> Colonies{get;}

      void Save(Ship ship);
      void Save(Campaign campaign);
      void Save(Sector sector);
      void Save(Pilot pilot);
      void Save(StarSystem starSystem);
      void Save(Colony colony);

      Ship LoadShip(int ship);
      Campaign Load(Campaign campaign);
      Sector Load(Sector sector);
      Pilot Load(Pilot pilot);
      StarSystem Load(StarSystem starSystem);
      Colony Load(Colony colony);
   }
}
