using System;
using SlipMap_Code_Library;

namespace WPF_SlipMap.Application
{
    [Serializable]
    public class Session
    {
        public int PilotSkill { get; set; }
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        public StarSystem Destination { get; set; }
    }
}