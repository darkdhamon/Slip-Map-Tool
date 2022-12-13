using System;
using System.IO;
using Newtonsoft.Json;
using SlipMap.NetFramework.Rewrite.Domain.Constants;
using SlipMap.NetFramework.Rewrite.Domain.Interfaces;

namespace SlipMap.NetFramework.Rewrite.Domain
{
    public class SaveFileManager<TObject> : ISaveFileManager<TObject> where TObject : class, new()
    {
        protected string FileName { get; set; }
        protected bool SaveBackUps { get; set; }
        public string SubDir { get; set; }

        public string DirectoryPath => $@"{SaveConstants.SaveDir}\{(!string.IsNullOrWhiteSpace(SubDir)?$@"{SubDir}\":string.Empty)}";
        public string FilePath => $@"{DirectoryPath}\{FileName}";

        public SaveFileManager() { }

        public SaveFileManager(string subDir)
        {
            SubDir = subDir;
        }

        public virtual void SaveFile(TObject saveObj)
        {
            if (string.IsNullOrWhiteSpace(FileName))
                FileName = typeof(TObject).Name;
            VerifyDirExists();
            //using var file = new StreamWriter(FilePath, append: false);
            var mapJson = JsonConvert.SerializeObject(saveObj);
            if (SaveBackUps)
                File.Copy(FilePath, $@"Backup\{DateTime.Now:yyyyMMddHHmm}\{FilePath}", true);
            File.WriteAllText(FilePath, mapJson);
        }

        public void SaveFileAs(TObject saveObj, string newFileName)
        {
            FileName = newFileName;
            SaveFile(saveObj);
        }
        protected void VerifyDirExists(string subDir = null)
        {
            if (!Directory.Exists(SaveConstants.SaveDir))
                Directory.CreateDirectory(SaveConstants.SaveDir);
            if (subDir != null && !Directory.Exists($@"{SaveConstants.SaveDir}\{subDir}"))
                Directory.CreateDirectory($@"{SaveConstants.SaveDir}\{subDir}");
        }

        public virtual TObject LoadFile(string filePath)
        {
            var mapJson = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<TObject>(mapJson);
        }
    }
}