#region File Header

// Slipstream Map - Gurps Ship Library - Class1.cs
// 
// Author: Bronze Harold Brown (bronz)
// File Created: 2016 07 23 4:30 PM
// File Edited: 2016 07 23 5:11 PM

#endregion

#region Using Directives

using System;

#endregion

namespace Gurps_Ship_Library
{
    [Flags]
    public enum AllowedSection
    {
        None = 0,
        ForwardHull = 1,
        MidHull = 2,
        AftHull = 4,
        Core = 8,
        Any = 15
    }
}