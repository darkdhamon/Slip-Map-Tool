using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlipMap.Domain.DataAccess;
using SlipMap.Model.Entities;

namespace SlipMapTest
{
   [TestClass]
   public class DataAccessTest
   {
      [TestMethod]
      public void CanSaveShip()
      {
         Ship ship = new Ship
         {
            Name = "Test Ship",
         };
         
         LocalFiles.Save(ship);
         Assert.IsTrue(File.Exists($"{LocalFiles.ShipDir}{ship.Name}.json"));
      }

      [TestMethod]
      public void CanLoadShip()
      {
         Ship ship = new Ship
         {
            Name = "Test Ship 2",
         };

         LocalFiles.Save(ship);
         var shipfromfile = LocalFiles.LoadShip("Test Ship 2");
         Assert.IsTrue(ship.Name==shipfromfile.Name);
      }

      [TestMethod]
      public void CanUpgrade()
      {
         LocalFiles.UpgradeApr2016("test.sm");
      }
   }

   [TestClass]
   public class PreserveOldFunctionalityTest
   {
      [TestMethod]
      public void CurrentSystemOverride()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void CustomSlipRoute()
      {
         throw new NotImplementedException();
      }
      [TestMethod]
      public void Organize()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void AdjustPilotSkill()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void LoadUser()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void PlotMultiJumpCourse()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void BlindJump()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void NavigationJump()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void LoadCampaign()
      {
         throw new NotImplementedException();
      }

      [TestMethod]
      public void CreateSector()
      {
         throw new NotImplementedException();
      }
   }
}
