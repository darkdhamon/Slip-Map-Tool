using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.Abstract;

namespace SlipMap.Domain.DataAccess
{
   public class Database
   {
      public Database(ISlipMapRepository slipMapRepository)
      {
         SlipMapRepository = slipMapRepository;
      }

      private ISlipMapRepository SlipMapRepository { get;}
      
   }
}
