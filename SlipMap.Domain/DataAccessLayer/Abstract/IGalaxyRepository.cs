using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.MapElements;

namespace SlipMap.Domain.DataAccessLayer.Abstract
{
    public interface IGalaxyRepository
    {
        IQueryable<Galaxy> GetGalaxies();
        IQueryable<StarSystem> GetStarSystems(int galaxyId);
    }
}
