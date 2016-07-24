using System;
using System.Collections.Generic;
using System.Linq;

namespace Gurps_Ship_Library
{
    public class Hull
    {
        private const int MaxSystemSlots = 6;
        private List<ShipSystem> _systems;
        public IEnumerable<ShipSystem> Systems => _systems ?? (_systems = new List<ShipSystem>());

        public virtual void AddSystem(ShipSystem system)
        {
            if (Systems.Sum(s => s.Slots) + system.Slots > MaxSystemSlots)
                throw new Exception("Too few system slots availible");
            (Systems as List<ShipSystem>)?.Add(system);
        }
    }
}