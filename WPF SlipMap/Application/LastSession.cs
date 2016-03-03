using System;
using SlipMap_Code_Library;

namespace WPF_SlipMap
{
  /// <summary>
  /// Last Session allows you to save details about the last session to resume from last time.
  /// </summary>
  [Serializable]
  public class LastSession
  {
    public string FileName { get; set; }
    public int PilotSkill { get; set; }
    public StarSystem Destination { get; set; }
  }
}