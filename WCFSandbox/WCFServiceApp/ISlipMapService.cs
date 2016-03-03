// WCFSandbox WCFServiceApp ISlipMapService.cs
// Created: 2016-03-03 9:48 AM
// Last Edited: 2016-03-03 10:05 AM
// 
// Author: Bronze Harold Brown

#region Imports

using System.Runtime.Serialization;
using System.ServiceModel;

#endregion

namespace WCFServiceApp
{
   // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISlipMapService" in both code and config file together.
   [ServiceContract]
   public interface ISlipMapService
   {
      [OperationContract]
      string GetData(int value);

      [OperationContract]
      StarSystem UpdateStarSystem(StarSystem composite);

      // TODO: Add your service operations here
   }


   // Use a data contract as illustrated in the sample below to add composite types to service operations.
   [DataContract]
   public class StarSystem
   {
      private string _systemDetails;
      private string _systemName;

      [DataMember]
      public bool SystemLocked { get; set; }

      [DataMember]
      public string SystemName
      {
         get { return _systemName ?? $"System {SystemID}"; }
         set { _systemName = value; }
      }

      [DataMember]
      public string GMNotes { get; set; }

      [DataMember]
      public string SystemDetails
      {
         get { return _systemDetails ?? (_systemDetails = "System Details have not been defined."); }
         set { _systemDetails = value; }
      }

      [DataMember]
      public int SystemID { get; set; }
   }
}