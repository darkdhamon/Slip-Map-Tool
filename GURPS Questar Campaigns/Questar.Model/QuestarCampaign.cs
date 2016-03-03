// GURPS Questar Campaigns Questar.Domain QuestarCampaign.cs
// Created: 2016-03-01 9:07 AM
// Last Edited: 2016-03-01 9:12 AM
// 
// Author: Bronze Harold Brown

using System;
using System.Collections.Generic;

namespace Questar.Model
{
   [Serializable]
   public class QuestarCampaign
   {
      public int ID { get; set; }
      public string CampaignName { get; set; }
      public List<Race> Type { get; set; }
      public List<Sector> Sectors { get; set; }
   }
   [Serializable]
   public class Sector
   {

   }
   [Serializable]
   public class Race
   {

   }
}