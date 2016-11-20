using SlipMap.Model.Entities;
using System.Collections.Generic;
using System.ServiceModel;

namespace SlipMapService
{
   // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISlipMapService" in both code and config file together.
   [ServiceContract]
   public interface ISlipMapService
   {

      [OperationContract]
      bool CheckConnection();

      [OperationContract]
      IEnumerable<Campaign> ListCampaigns();

      [OperationContract]
      IEnumerable<Sector> ListSectors();

      [OperationContract(Action = "ListSectorsByCampaign")]
      IEnumerable<Sector> ListSectors(int campaignID);

      [OperationContract]
      IEnumerable<Ship> ListShips(int campaignId);

      [OperationContract]
      IEnumerable<Pilot> ListPilots(int shipId);

      [OperationContract]
      IEnumerable<StarSystem> ListStarSystems(int campaignID);

      [OperationContract(Action = "GetStarSystemByID")]
      StarSystem GetStarSystem(int sectorId, int id);

      [OperationContract]
      Pilot GetPilot(int id);

      [OperationContract(Action = "GetPilotByName")]
      Pilot GetPilot(string name);

      
   }


}
