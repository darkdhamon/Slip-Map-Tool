using System;

namespace Gurps_Ship_Library
{
    public class AftHull : Hull
    {
        public override void AddSystem(ShipSystem system)
        {
            if (system.AllowedSection.HasFlag(AllowedSection.AftHull)) base.AddSystem(system);
            else throw new Exception("System not allowed in this section.");
        }
    }
}