using SlipMap.Domain.DataAccess;
using SlipMap.Domain.ExtentionMethods;
using SlipMap.Model.Entities;

namespace SlipMap.Domain.Game
{
   partial class Game
   {
      private Campaign _campaign;
      private Ship _ship;
      private Sector _sector;
      private Pilot _pilot;
      private Session _session;

      public Session Session
      {
         get { return _session??(_session = new Session()); }
         set { _session = value; }
      }

      public Campaign Campaign
      {
         get { return _campaign; }
         set
         {
            _campaign = value;
            Session.Campaign = value.Name;
         }
      }
      public Ship Ship
      {
         get { return _ship; }
         set
         {
            _ship = value;
            Session.Ship = value.Name;
         }
      }
      public Sector Sector
      {
         get { return _sector; }
         set
         {
            _sector = value;
            Session.Sector = value.Name;
            _sector.FillConnectedStarSystems();
         }
      }
      public Pilot Pilot
      {
         get { return _pilot; }
         set
         {
            _pilot = value;
            Session.Pilot = value.Name;
         }
      }


   }
}
