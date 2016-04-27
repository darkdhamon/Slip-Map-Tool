// Slipstream Map WPF SlipMap Session.cs
// Created: 2016-03-03 12:55 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Security.Principal;
using SlipMap_Code_Library;

#endregion

namespace WPF_SlipMap
{
   /// <summary>
   ///    Last Session allows you to save details about the last session to resume from last time.
   /// </summary>
   [Serializable]
   public class Session
   {
      private string _displayName;
      public string FileName { get; set; }
      public int PilotSkill { get; set; }
      public StarSystem Destination { get; set; }

      public string DisplayName
      {
         get
         {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null)
               return _displayName ?? (_displayName = windowsIdentity.Name);
            return _displayName ?? (_displayName = $"User{DateTime.Now.ToString("yyyyMMdd")}");
         }
         set { _displayName = value; }
      }
   }
}