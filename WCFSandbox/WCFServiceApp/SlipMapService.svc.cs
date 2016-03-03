using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WCFServiceApp
{
   // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SlipMapService" in code, svc and config file together.
   // NOTE: In order to launch WCF Test Client for testing this service, please select SlipMapService.svc or SlipMapService.svc.cs at the Solution Explorer and start debugging.
   public class SlipMapService : ISlipMapService
   {
      public string GetData(int value)
      {
         return string.Format("You entered: {0}", value);
      }

      public StarSystem UpdateStarSystem(StarSystem composite)
      {
         if (composite == null)
         {
            throw new ArgumentNullException("composite");
         }
         if (composite.SystemLocked)
         {
            composite.SystemName += "Suffix";
         }
         return composite;
      }
   }
}
