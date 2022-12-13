using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlipMap.NetFramework.Rewrite.Model
{
    public class SlipMap
    {
        public string SectorName { get; set; }

        [JsonIgnore]
        public HashSet<StarSystem> StarSystems { get; set; } = new HashSet<StarSystem>();
        /// <summary>
        /// This is the ID of the last StarSystem created by StarWin
        /// </summary>
        public int LastSystemID { get; set; }
    }
}
