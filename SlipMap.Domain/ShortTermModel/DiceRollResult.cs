using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.Domain.ShortTermModel
{
   public class DiceRollResult
   {
      public string DiceFormat { get; set; }
      public List<int> Rolled { get; set; }
      public int Total => Rolled.Sum();
   }
}
