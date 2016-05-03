using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.Entities;

namespace SlipMap.Domain.ShortTermModel
{
   class RouteNode
   {
      public RouteNode ParentNode { get; set; }
      public StarSystem StarSystem { get; set; }
   }
}
