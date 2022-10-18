using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.Model.MapElements
{
    public class StarShip
    {
        private List<SlipRoute>? _knownSlipRoutes;
        public int ID { get; set; }
        public string Name { get; set; }
        public string? Notes { get; set; }

        public StarSystem CurrentStarSystem { get; set; }

        public List<SlipRoute> KnownSlipRoutes
        {
            get => _knownSlipRoutes??=new List<SlipRoute>();
            set => _knownSlipRoutes = value;
        }
    }
}
