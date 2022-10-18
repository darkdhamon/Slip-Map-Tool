using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.Model.MapElements
{
    public class StarSystem
    {
        private List<StarSystem> _connectedSystems = null!;
        public int ID { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }

        public List<StarSystem> ConnectedSystems
        {
            get => _connectedSystems??= new List<StarSystem>();
            set => _connectedSystems = value;
        }

        public override string ToString()
        {
            return $"{Name.IfNullOrWhiteSpace("Unnamed System")} ({ID})";
        }
    }
}
