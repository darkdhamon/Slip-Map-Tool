#region File Header

// Slipstream Map - Gurps Ship Library - Ship.cs
// 
// Author: Bronze Harold Brown (bronz)
// File Created: 2016 07 23 5:16 PM
// File Edited: 2016 07 23 5:17 PM

#endregion

#region Using Directives

using System;

#endregion

namespace Gurps_Ship_Library
{
    public class Ship
    {
        private ShipSystem _coreSystem1;
        private ShipSystem _coreSystem2;
        public string Name { get; set; }

        public ShipSystem CoreSystem1
        {
            get { return _coreSystem1; }
            set
            {
                if (value.AllowedSection.HasFlag(AllowedSection.Core)) _coreSystem1 = value;
                else throw new Exception("System not allowed in this section.");
            }
        }

        public ShipSystem CoreSystem2
        {
            get { return _coreSystem2; }
            set
            {
                if (value.AllowedSection.HasFlag(AllowedSection.Core)) _coreSystem2 = value;
                else throw new Exception("System not allowed in this section.");
            }
        }

        public ForwardHull ForwardHull { get; set; }
        public MidHull MidHull { get; set; }
        public AftHull AftHull { get; set; }
    }
}