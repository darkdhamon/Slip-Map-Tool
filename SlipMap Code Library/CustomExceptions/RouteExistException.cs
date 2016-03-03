// Slipstream Map SlipMap Code Library RouteExistException.cs
// Created: 2016-02-24 3:14 PM
// Last Edited: 2016-02-24 3:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;

#endregion

namespace SlipMap_Code_Library
{
  /// <summary>
  ///   Custom Exception
  /// </summary>
  public class RouteExistException : Exception
  {
    public RouteExistException() : base("Slip Route already exist.")
    {
    }
  }
}