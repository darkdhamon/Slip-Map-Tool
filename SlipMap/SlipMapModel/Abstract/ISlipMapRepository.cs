using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMapModel.Entities;

namespace SlipMapModel.Abstract
{
   interface ISlipMapRepository
   {
      List<StarSystem> StarSystems { get; }
      List<Ship> Ships { get; }
      List<Sector> Sectors { get; }
      List<Colony> Colonies { get; }
      List<Pilot> Pilots { get; }
      List<User> Users { get; }
      List<UserRole> UserRoles { get; } 
   }
}
