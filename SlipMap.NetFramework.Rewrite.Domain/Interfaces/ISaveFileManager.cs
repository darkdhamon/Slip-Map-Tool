using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlipMap.NetFramework.Rewrite.Domain.Interfaces
{
    internal interface ISaveFileManager<TObject> where TObject : class, new()
    {
        void SaveFile(TObject saveObj);
        void SaveFileAs(TObject saveObj, string newFileName);
        TObject LoadFile(string filePath);
    }
}
