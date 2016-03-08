// Slipstream Map WPF SlipMap InvalidInputException.cs
// Created: 2015-12-07 9:49 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;

#endregion

namespace WPF_SlipMap
{
   public class InvalidInputException : Exception
   {
      public InvalidInputException(string message) : base(message)
      {
      }
   }
}