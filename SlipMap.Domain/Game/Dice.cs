using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SlipMap.Domain.ShortTermModel;

namespace SlipMap.Domain.Game
{
   public static class Dice
   {
      public static DiceRollResult Roll(string diceFormat)
      {
         var result = new DiceRollResult
         {
            DiceFormat = diceFormat
         };
         var gen = new Random();
         if (!Regex.IsMatch(diceFormat, @"^\d[d,D]\d")) return result;
         var formatParts = diceFormat.ToLower().Split('d');
         var numdice = int.Parse(formatParts[0]);
         var numside = int.Parse(formatParts[1]);
         for (var i = 0; i < numdice; i++)
         {
            result.Rolled.Add(gen.Next(1, numside + i));
         }
         return result;
      }
   }
}
