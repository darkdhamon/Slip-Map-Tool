using System;
using System.Collections.Generic;
using System.Linq;

namespace SlipMap_Code_Library
{
    [Serializable]
    public class StarSystem{
        public int ID { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        private List<StarSystem> _connectedSystems;
        public List<StarSystem> ConnectedSystems => _connectedSystems ?? (_connectedSystems = new List<StarSystem>());

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? $"System {ID}" : $"{Name}  ({ID})";
        }

        /// <summary>
        /// Organize List
        /// </summary>
        public void Organize()
        {
            // Sort all Connected systems by Name and ID
            _connectedSystems = ConnectedSystems.OrderBy(system => system.Name).ThenBy(system => system.ID).ToList();
        }
    }
}
