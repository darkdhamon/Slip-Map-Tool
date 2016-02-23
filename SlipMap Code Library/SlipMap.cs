using System;
using System.Collections.Generic;
using System.Linq;

namespace SlipMap_Code_Library
{
    [Serializable]
    public class SlipMap
    {
        public StarSystem CurrentSystem
        {
            get { return _currentSystem; }
            set
            {
                if (value == null) return;
                _currentSystem = Systems.FirstOrDefault(system => system.ID == value.ID) ?? value; 
                if(Systems.All(system => system.ID != value.ID))Systems.Add(_currentSystem);
            }
        }

        private List<StarSystem> _systems;
        private StarSystem _currentSystem;

        public List<StarSystem> Systems => _systems ?? (_systems = new List<StarSystem>());

        public int LastSystemID { get; set; }

        public void Clean()
        {
            foreach (var starSystem in Systems)
            {
                var uniqueRoutes = starSystem.ConnectedSystems.GroupBy(system=>system.ID).Select(grp=>grp.First()).ToList();
                starSystem.ConnectedSystems.RemoveAll(system => true );
                starSystem.ConnectedSystems.AddRange(uniqueRoutes);
                starSystem.Clean();
            }
            _systems = Systems.OrderBy(system => system.Name).ThenBy(system => system.ID).ToList();
            

        }
    }
}