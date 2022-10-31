using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using SlipMap.Model.MapElements;

namespace SlipMap.Domain.Generators.Images.Abstract
{
    public interface IGalaxyImageGenerator
    {
        MemoryStream TopView(Galaxy galaxy, double scale = 1);
        MemoryStream SideView(Galaxy galaxy, double scale = 1);
    }
}
