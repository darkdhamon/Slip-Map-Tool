using System;

namespace Gurps_Ship_Library
{
    public class ForwardHull : Hull
    {
        public override void AddSystem(ShipSystem system)
        {
            if (system.AllowedSection.HasFlag(AllowedSection.ForwardHull)) base.AddSystem(system);
            else throw new Exception("System not allowed in this section.");
        }
    }
}