using System.Collections.Generic;
using System.IO;
using System.Linq;
using SlipMap.Model.Entities;
using SlipMap_Code_Library;
using StarSystem = SlipMap.Model.Entities.StarSystem;

namespace SlipMap.Domain.DataAccess
{
   public static class LocalFiles
   {
      static LocalFiles()
      {
         if (!Directory.Exists(CampaignDir)) Directory.CreateDirectory(CampaignDir);
         if (!Directory.Exists(ShipDir)) Directory.CreateDirectory(ShipDir);
         if (!Directory.Exists(SectorDir)) Directory.CreateDirectory(SectorDir);
         
      }
      public const string CampaignDir = "Saves/Campaign/";
      public const string ShipDir = "Saves/Ships/";
      public const string SectorDir = "Saves/Sectors/";

      /// <summary>
      /// Old save files must be updated to work with versions of the application after 4/27/2016
      /// </summary>
      /// <param name="filename"></param>
      /// <returns></returns>
      public static void UpgradeApr2016(string filename)
      {
         var drive = new SlipDrive {FileName = filename};
         drive.LoadSlipMap();
         var sector = new Sector
         {
            LastSystemID = drive.LastSystemId,
            Name = drive.FileName.Replace(".sm", ""),
            Systems = drive.VisitedSystems.Select(s => new StarSystem
            {
               Name = s.Name,
               ConnectedSystemIds = s.ConnectedSystems.Select(sys => sys.ID).ToList(),
               GMNotes = s.Notes,
               StarWinId = s.ID
            }).ToList()
         };
         var ship = new Ship
         {
            Name = "Unnamed Vessel",
            CurrentLocation = new StarSystem
               {
                  Name = drive.CurrentSystem.Name,
                  ConnectedSystemIds = drive.CurrentSystem.ConnectedSystems.Select(s => s.ID).ToList(),
                  GMNotes = drive.CurrentSystem.Notes,
                  StarWinId = drive.CurrentSystem.ID
               }
         };
         var campaign = new Campaign
         {
            Name = sector.Name,
            TrackedShipNames = {ship},
            TrackedSectors = {sector}
         };

         Save(ship);
         Save(sector);
         Save(campaign);
      }

      public static void Save(Session session)
      {
         JsonSerialization.WriteToJsonFile("LastSession.json", session);
      }
      public static void Save(Campaign campaign)
      {
         JsonSerialization.WriteToJsonFile($"{CampaignDir}{campaign.Name}.json", campaign);
      }
      public static void Save(Ship ship)
      {
         JsonSerialization.WriteToJsonFile($"{ShipDir}{ship.Name}.json", ship);
      }
      public static void Save(Sector sector)
      {
         JsonSerialization.WriteToJsonFile($"{SectorDir}{sector.Name}.json", sector);
      }
      

      public static Ship LoadShip(string ship)
      {
         return JsonSerialization.ReadFromJsonFile<Ship>($"{ShipDir}{ship}.json");
      }
      public static Campaign LoadCampaign(string campaign)
      {
         return JsonSerialization.ReadFromJsonFile<Campaign>($"{CampaignDir}{campaign}.json");
      }
      public static Sector LoadSector(string sector)
      {
         return JsonSerialization.ReadFromJsonFile<Sector>($"{SectorDir}{sector}.json");
      }
      public static Session LoadSession()
      {
         return JsonSerialization.ReadFromJsonFile<Session>("LastSession.json");
      }

      public static List<string> Ships()
      {
         var saveDir = new DirectoryInfo(ShipDir);
         var files = saveDir.GetFiles("*.json"); // This retrieves a list of files with the extintion .sm
         return files.Select(file => file.Name.Replace(".json", "")).ToList();
      }
      public static List<string> Sectors()
      {
         var saveDir = new DirectoryInfo(SectorDir);
         var files = saveDir.GetFiles("*.json"); // This retrieves a list of files with the extintion .sm
         return files.Select(file => file.Name.Replace(".json", "")).ToList();
      }
      public static List<string> Campaigns()
      {
         var saveDir = new DirectoryInfo(CampaignDir);
         var files = saveDir.GetFiles("*.json"); // This retrieves a list of files with the extintion .sm
         return files.Select(file => file.Name.Replace(".json","")).ToList();
      }
   }
}
