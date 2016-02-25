// Slipstream Map SlipMap Code Library SlipMap.cs
// Created: 2015-12-07 10:13 PM
// Last Edited: 2016-02-24 3:18 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SlipMap_Code_Library
{
  [Serializable]
  internal class SlipMap
  {

    public StarSystem CurrentSystem
    {
      get { return _currentSystem; }
      set
      {
        //Do not allow null
        if (value == null) return;
        //Do not allow duplicates and set current system.
        _currentSystem = Systems.FirstOrDefault(system => system.ID == value.ID) ?? value;
        //Add current system to all systems if it is a new system.
        if (Systems.All(system => system.ID != _currentSystem.ID)) Systems.Add(_currentSystem);
      }
    }

    
    public List<StarSystem> Systems => _systems ?? (_systems = new List<StarSystem>());

    public int LastSystemID { get; set; }

    /// <summary>
    ///   This is the Number of StarSystems in the SlipMap not the Number Of StarSystems Visited.
    /// </summary>
    public int Count => LastSystemID + 1;

    /// <summary>
    /// This method oganizes the slip map
    /// </summary>
    public void CleanAndOrganize()
    {
      
      foreach (var starSystem in Systems)
      {
        //remove duplicate routes from each star system.
        var uniqueRoutes = starSystem.ConnectedSystems.GroupBy(system => system.ID).Select(grp => grp.First()).ToList();
        starSystem.ConnectedSystems.RemoveAll(system => true);
        starSystem.ConnectedSystems.AddRange(uniqueRoutes);
        starSystem.Organize();
      }
      // Sort systems by name and ID
      _systems = Systems.OrderBy(system => system.Name).ThenBy(system => system.ID).ToList();
    }

    #region Private Fields

    private StarSystem _currentSystem;
    private List<StarSystem> _systems;

    #endregion
  }
}