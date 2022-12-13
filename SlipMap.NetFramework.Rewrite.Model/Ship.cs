using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.NetFramework.Rewrite.Model
{
    public class Ship
    {
        public int CurrentStarSystem { get; set; }
        public string ShipName { get; set; }
        public string CurrentSectorMapName { get; set; }
        public string SlipPilotSkill { get; set; }
        /// <summary>
        /// Key:SectorMapName
        /// Value: List of SystemID's visited on that map
        /// </summary>
        public Dictionary<string, List<int>> VisitedSystemIds { get; set; }
    }
}
