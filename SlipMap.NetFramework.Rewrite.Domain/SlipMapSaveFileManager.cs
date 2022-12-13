using System.IO;
using Newtonsoft.Json;
using SlipMap.NetFramework.Rewrite.Model;

namespace SlipMap.NetFramework.Rewrite.Domain
{
    public class SlipMapSaveFileManager:SaveFileManager<Model.SectorMap>
    {
        public override void SaveFile(Model.SectorMap saveObj)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                if (string.IsNullOrWhiteSpace(saveObj.SectorName))
                {
                    throw new IOException("File Name is Required, and Default Name cannot be determined");
                }
                FileName = $"{saveObj.SectorName}.SectorMap.json";
            }
            base.SaveFile(saveObj);
            VerifyDirExists($"{saveObj.SectorName}-Systems");
            var systemManager = new SaveFileManager<StarSystem>($"{saveObj.SectorName}-Systems");
            foreach (var starSystem in saveObj.StarSystems)
            {
                systemManager.SaveFileAs(starSystem,$"S-{starSystem.Id}.system.json");
            }
        }

        public override Model.SectorMap LoadFile(string filePath)
        {
            var slipMap = base.LoadFile(filePath);
            var starSystemManager = new SaveFileManager<StarSystem>($"{slipMap.SectorName}-Systems");
            var starSystemDirectory = new DirectoryInfo(starSystemManager.DirectoryPath);
            var starFiles = starSystemDirectory.GetFiles("*.system.json");
            foreach (var starFile in starFiles)
            {
                var starJson = File.ReadAllText(starFile.FullName);
                slipMap.StarSystems.Add(JsonConvert.DeserializeObject<StarSystem>(starJson));
            }
            return slipMap;
        }
    }
}
