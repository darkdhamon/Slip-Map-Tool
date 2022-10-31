using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SlipMap.Model.MapElements;

namespace SlipMap.Domain.Generators.Images.Abstract
{
    public interface IGalaxyImageGenerator
    {
        MemoryStream TopView(Galaxy galaxy);
        MemoryStream SideView(Galaxy galaxy);
    }
}
