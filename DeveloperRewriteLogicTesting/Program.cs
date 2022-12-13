using System.Collections.Generic;
using SlipMap.NetFramework.Rewrite.Domain;
using SlipMap.NetFramework.Rewrite.Model;

namespace DeveloperRewriteLogicTesting
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var map = new SlipMap.NetFramework.Rewrite.Model.SectorMap
            {
                SectorName = "Test",
                StarSystems = new List<StarSystem>
                {
                    new StarSystem()
                    {
                        Id = 1
                    },
                    new StarSystem()
                    {
                        Id = 2
                    }
                },
                LastSystemID = 0
            };
            var manager = new SlipMapSaveFileManager();
            manager.SaveFile(map);
            var map2 = manager.LoadFile("sectorfiles\\Test.SectorMap.json");
        }
    }
}
