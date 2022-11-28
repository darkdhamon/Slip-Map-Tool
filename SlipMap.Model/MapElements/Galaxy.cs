using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.Model.MapElements
{
    public class Galaxy
    {
        private List<StarSystem>? _starSystems;
        public string Name { get; set; } = null!;

        public List<StarSystem> StarSystems
        {
            get => _starSystems??= new List<StarSystem>();
            set => _starSystems = value;
        }
    }
}
