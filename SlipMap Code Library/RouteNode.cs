// Slipstream Map SlipMap Code Library RouteNode.cs
// Created: 2016-02-24 3:15 PM
// Last Edited: 2016-02-24 3:16 PM
// 
// Author: Bronze Harold Brown

namespace SlipMap_Code_Library
{
  /// <summary>
  ///   RouteNode is used to for searching for routes in the slipmap.
  /// </summary>
  public class RouteNode
  {
    public RouteNode ParentNode { get; set; }
    public StarSystem StarSystem { get; set; }
  }
}