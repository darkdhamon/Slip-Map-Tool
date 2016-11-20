using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using SlipMap.Model.Entities;

namespace SlipMapService
{
   // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SlipMapService" in code, svc and config file together.
   // NOTE: In order to launch WCF Test Client for testing this service, please select SlipMapService.svc or SlipMapService.svc.cs at the Solution Explorer and start debugging.
   public class SlipMapService : ISlipMapService
   {
      public bool CheckConnection()
      {
         throw new NotImplementedException();
      }

      public IEnumerable<Campaign> ListCampaigns()
      {
         throw new NotImplementedException();
      }

      public IEnumerable<Sector> ListSectors()
      {
         throw new NotImplementedException();
      }

      public IEnumerable<Sector> ListSectors(int campaignID)
      {
         throw new NotImplementedException();
      }

      public IEnumerable<Ship> ListShips(int campaignId)
      {
         throw new NotImplementedException();
      }

      public IEnumerable<Pilot> ListPilots(int shipId)
      {
         throw new NotImplementedException();
      }

      public IEnumerable<StarSystem> ListStarSystems(int campaignID)
      {
         throw new NotImplementedException();
      }

      public StarSystem GetStarSystem(int sectorId, int id)
      {
         throw new NotImplementedException();
      }

      public Pilot GetPilot(int id)
      {
         throw new NotImplementedException();
      }

      public Pilot GetPilot(string name)
      {
         throw new NotImplementedException();
      }
   }
}
