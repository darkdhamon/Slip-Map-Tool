using System.Collections.Generic;

namespace SlipMap.NetFramework.Rewrite.Model
{
    public class StarSystem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public HashSet<int> ConnectedSystemIds { get; set; } = new HashSet<int>();
    }
}