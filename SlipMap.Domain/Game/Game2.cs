using SlipMap.Domain.DataAccess;
using SlipMap.Domain.ExtentionMethods;

namespace SlipMap.Domain.Game
{
   partial class Game
   {
      public Game()
      {
         LoadSession();
      }

      private void LoadSession()
      {
         Session = LocalFiles.LoadSession();
         if (Session == null) return;
         Campaign = LocalFiles.LoadCampaign(Session.Campaign);
         Ship = LocalFiles.LoadShip(Session.Ship);
         Sector = LocalFiles.LoadSector(Session.Sector);
         Pilot = Ship.Pilot(Session.Pilot);
      }

      public void ChangeShip(string name)
      {
         Ship?.Save();
         Ship = LocalFiles.LoadShip(name);
         Session.Ship = Ship.Name;
         Session.Save();
      }
      public void ChangeSector(string name)
      {
         Sector?.Save();
         Sector = LocalFiles.LoadSector(name);
         Session.Sector = Sector.Name;
         Session.Save();
      }
      public void ChangeCampaign(string name)
      {
         Campaign?.Save();
         Campaign = LocalFiles.LoadCampaign(name);
         Session.Campaign = Campaign.Name;
         Session.Save();
      }
      public void ChangePilot(string name)
      {
         Pilot = Ship?.Pilot(name);
         Session.Pilot = Pilot?.Name;
         Session.Save();
      }
   }
}
