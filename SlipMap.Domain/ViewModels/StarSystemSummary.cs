using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.MapElements;

namespace SlipMap.Domain.ViewModels
{
    public class StarSystemSummary
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public RadialMapCoordinates Coordinates { get; set; }
    }
}
