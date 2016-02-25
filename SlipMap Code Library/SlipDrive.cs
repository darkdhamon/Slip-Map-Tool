// Slipstream Map SlipMap Code Library SlipDrive.cs
// Created: 2015-12-08 12:41 AM
// Last Edited: 2016-02-17 6:17 AM
// 
// Author: Bronze Harold Brown

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SlipMap_Code_Library
{
  /// <summary>
  ///   Slip Drive is the controller for the application.
  /// </summary>
  public class SlipDrive
  {
    /// <summary>
    ///   Sets Save Directory to be SectorFiles. This is not somethingthin I want the user to change.
    /// </summary>
    public SlipDrive()
    {
      SaveDir = "SectorFiles";
    }

    /// <summary>
    ///   The file name is going to be the name of the sector.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    ///   SaveDir is set in the contructor this can not be changed
    /// </summary>
    private string SaveDir { get; }

    /// <summary>
    ///   This is the Model that gets saved to disk
    /// </summary>
    private SlipMap Map { get; set; }

    /// <summary>
    ///   This is the jump message that appears when the a jump is completed.
    /// </summary>
    public string NavigationJumpMessage { get; set; }

    /// <summary>
    ///   This is a readonly varible that gets the ID of the Last system availible on the Map.
    ///   Note this is not the ID of the largest instantiated system only the last ID that was defined
    ///   by the user. It is the ID of the last system created in the application Starwin.
    /// </summary>
    public int LastSystemId => Map.LastSystemID;

    /// <summary>
    ///   This is the location of the ship in the current system.
    /// </summary>
    public StarSystem CurrentSystem => Map?.CurrentSystem;

    /// <summary>
    ///   This is a list of all systems in the Map. The way this application is designed with the exception
    ///   of GM overrides the The Slipstream Drive has to visit a star system in order to exist in the system
    /// </summary>
    public IEnumerable<StarSystem> VisitedSystems => Map?.Systems;

    /// <summary>
    ///   Autosave is on by default
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    ///   Primarily used for skill rolls, this option allows you to see what was rolled
    /// </summary>
    /// <param name="die1"></param>
    /// <param name="die2"></param>
    /// <param name="die3"></param>
    /// <returns></returns>
    private static int DiceRoll3D6(out int die1, out int die2, out int die3)
    {
      var possibilities = new Random();
      die1 = possibilities.Next(1, 7);
      die2 = possibilities.Next(1, 7);
      die3 = possibilities.Next(1, 7);
      return die1 + die2 + die3;
    }

    /// <summary>
    ///   Primarily used for skill rolls, this option only allows you to see the result of what was rolled.
    ///   use this when you do not care what the actual dice are.
    /// </summary>
    /// <returns></returns>
    private static int DiceRoll3D6()
    {
      int a, b, c;
      return DiceRoll3D6(out a, out b, out c);
    }

    /// <summary>
    ///   This is the public access to Blind Jump. It checks to see if the pilot breaks the ship
    ///   before it actually runs the private blindjump method.
    /// </summary>
    /// <param name="computer"></param>
    /// <param name="pilotSkillLevel"></param>
    public void BlindJump(bool computer, int pilotSkillLevel)
    {
      var succeeded = true;
      if (!computer)
      {
        succeeded = OrganicSkillResultOnBlindJump(pilotSkillLevel);
      }
      // continue with blind jump
      if (succeeded) BlindJump(computer);
    }

    /// <summary>
    ///   This method determines what happens to the pilot or ship based on the pilot skill roll.
    /// </summary>
    /// <param name="pilotSkillLevel"></param>
    private bool OrganicSkillResultOnBlindJump(int pilotSkillLevel)
    {
      int die1, die2, die3;
      var skillRoll = DiceRoll3D6(out die1, out die2, out die3);
      NavigationJumpMessage =
        $"Rolled {skillRoll} Vs. {pilotSkillLevel} with the following dice {die1}, {die2}, {die3}";

      // if a critical or automatic fail condition occures
      if (skillRoll == 18 || skillRoll - pilotSkillLevel > 9)
      {
        NavigationJumpMessage = "Critical Failure: " + NavigationJumpMessage +
                                "; Unfortunately with this roll you did not go anywhere. You now have a broken Grav lens and can not enter the slipstream untill it is repaired";
        return false;
      }
      if (skillRoll == 17)
      {
        NavigationJumpMessage = "Automatic Failure: " + NavigationJumpMessage +
                                "; Unfortunately with this roll you did not go anywhere as the slip portal failed to open, Please Try Again";
        return false;
      }
      if (skillRoll > pilotSkillLevel)
      {
        NavigationJumpMessage += "\nBlind jump succeeded. however the pilot has been knocked unconsious/offline.";
      }

      var timeRoll = DiceRoll3D6();
      var hours = timeRoll - (pilotSkillLevel - skillRoll);
      //Tell the user the amount of time it took to complete the jump. Hours may not be less than 0
      NavigationJumpMessage += $"\n It took {(hours < 0 ? 0 : hours)} hours";
      return true;
    }

    /// <summary>
    ///   This is the actual blind jump method.
    /// </summary>
    /// <param name="computer"></param>
    private void BlindJump(bool computer)
    {
      // Origin is the system we are leaving
      var origin = CurrentSystem;
      // Set destination to origin for the purpose of the while loop
      var destination = origin;

      while (origin == destination)
      {
        var possibilities = new Random();
        // A computer will always jump to a random system in the sector, with out being drawn to a famililar place
        // A organic is however drawn back to a known system about a third of the time.
        if (computer || possibilities.Next(0, 3) != 0)
        {
          // Select a system ID the plus one allow makes sure that the last system is included.
          var destinationId = possibilities.Next(0, Map.Count);

          // Set Destination. If Destination does not exist create it.
          destination = Map.Systems.FirstOrDefault(s => s.ID == destinationId) ?? new StarSystem
          {
            ID = destinationId
          };
        }
        else
        {
          //Set destination
          destination = Map.Systems.ElementAt(possibilities.Next(0, Map.Systems.Count));
        }
      }

      // Create sliproute
      try
      {
        CreateSlipRoute(origin, destination);
      }
      catch (RouteExistException)
      {
        // catch duplicate slip route error and ignore it.
        NavigationJumpMessage += "\nYou blindly follow a known route.";
      }
      // set current system
      Map.CurrentSystem = destination;
      // Auto Save
      if (AutoSave)
      {
        SaveSlipMap();
      }
    }


    /// <summary>
    ///   Create a sliproute between the two known systems.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <exception cref="NullReferenceException">Origin and Destination may not be null.</exception>
    public void CreateSlipRoute(StarSystem origin, StarSystem destination)
    {
      // both systems must exist
      if (origin == null || destination == null)
        throw new NullReferenceException("Both systems have to be visited at least once.");
      // ignore existing routes
      if (origin.ConnectedSystems.Contains(destination) || destination.ConnectedSystems.Contains(origin))
        throw new RouteExistException();
      // Create Connections. Connections must be two way.
      origin.ConnectedSystems.Add(destination);
      destination.ConnectedSystems.Add(origin);
    }

    /// <summary>
    ///   Navigation jumps are used to follow known routes to a desired destination.
    /// </summary>
    /// <param name="computer"></param>
    /// <param name="destination"></param>
    /// <param name="pilotSkillLevel"></param>
    public void NavigationJump(bool computer, StarSystem destination, int pilotSkillLevel)
    {
      // If a computer pilots the slips stream things are realitively simple. 50/50 chance of making it to the desired system.
      var possibilities = new Random();
      if (computer)
      {
        ComputerNavigationJump(destination, possibilities);
        return;
      }

      OrganicNavigationJump(destination, pilotSkillLevel, possibilities);
    }

    private void OrganicNavigationJump(StarSystem destination, int pilotSkillLevel, Random possibilities)
    {
//If a biological organizim pilots the slipstream then things start getting complicated.
      //Roll for Pilot Skill
      int die1, die2, die3;
      var sum = DiceRoll3D6(out die1, out die2, out die3);
      NavigationJumpMessage = $"Rolled {sum} Vs. {pilotSkillLevel} with the following dice {die1}, {die2}, {die3}";

      // Check for critical success, where Critical success is a 3 or success by 10 or more.
      if (sum == 3 || pilotSkillLevel - sum > 9)
      {
        OrganicJump(destination,possibilities,0,"Critical Success");
      }

      // Check for critical failure where Critical Failure is an 18 or fail by 10 or more. This also results in a broken Grav Lens.
      else if (sum == 18 || sum - pilotSkillLevel > 9)
      {
        NavigationJumpMessage = "Critical Failure: " + NavigationJumpMessage +
                                "; Unfortunately with this roll you did not go anywhere. " +
                                "You now have a broken Grav lens and can not enter the slipstream untill it is repaired";
      }

      // Check for automatic success. If the pilot is not skilled in piloting then 4 is an automatic success.
      else if (sum == 4)
      {
        OrganicJump(destination, possibilities, 5, "Automatic Success");
      }

      //Check for successs
      else if (sum < pilotSkillLevel)
      {
        OrganicJump(destination,possibilities,10,"Success");
      }

      //check for Failure
      else if (sum > pilotSkillLevel)
      {
        OrganicJump(destination,possibilities,50,"Failure");
      }
      else if (sum == 17)
      {
        NavigationJumpMessage = "Automatic Failure: " + NavigationJumpMessage +
                                "; Unfortunately with this roll you did not go anywhere as the slip portal failed to open, Please Try Again";
      }
    }

    /// <summary>
    /// This is what happens on on any roll that is not a critical Fail roll or auto failure.
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="possibilities"></param>
    /// <param name="prob">This is the probability of a blind jump</param>
    /// <param name="successType"></param>
    private void OrganicJump(StarSystem destination, Random possibilities, int prob, string successType)
    {
      if (possibilities.Next(0, 100) < 100 - prob)
      {
        Map.CurrentSystem = destination;
        NavigationJumpMessage = $"{successType}: {NavigationJumpMessage}; Congrats! {100-prob}% of all {successType} arrive at the desired location.";
      }
      else
      {
        BlindJump(false);
        NavigationJumpMessage = $"{successType}: {NavigationJumpMessage}; However {prob}% of all " +
                                $"{successType} rolls result in a blind jump.";
      }
    }

    /// <summary>
    /// This is what happens when a computer Navigates
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="possibilities"></param>
    private void ComputerNavigationJump(StarSystem destination, Random possibilities)
    {
      if (possibilities.Next(0, 2) == 0)
      {
        NavigationJumpMessage += "\nThe computer has lost it's way in the Slip Stream Network.";
        BlindJump(true);
      }
      else
      {
        Map.CurrentSystem = destination;
        NavigationJumpMessage += "\nThe computer has navigated to the correct destination.";
      }
    }

    /// <summary>
    ///   Create a slip map, specify the number of systems and the current system ID is optional. if current system is left
    ///   null then current system will be randomly assigned
    /// </summary>
    /// <param name="lastSystemID"></param>
    /// <param name="currentSystemID"></param>
    public void CreateSlipMap(int lastSystemID, int? currentSystemID = null)
    {
      var possibilities = new Random();
      Map = new SlipMap
      {
        LastSystemID = lastSystemID,
        CurrentSystem = new StarSystem {ID = currentSystemID ?? possibilities.Next(0, lastSystemID + 1)}
      };
      SaveSlipMap();
    }

    /// <summary>
    /// This allows you to override the current system and set it to a new one.
    /// </summary>
    /// <param name="systemID"></param>
    /// <param name="overrideSafety"></param>
    public void ManualChangeCurrentSystem(int systemID, bool overrideSafety)
    {
      var selectedSystem = Map.Systems.FirstOrDefault(s => s.ID == systemID);
      if (selectedSystem == null && overrideSafety)
      {
        selectedSystem = new StarSystem
        {
          ID = systemID
        };
      }
      else if (selectedSystem == null)
      {
        throw new Exception("This Requested System does not exist.");
      }

      Map.CurrentSystem = selectedSystem;
      SaveSlipMap();
    }

    /// <summary>
    /// This method Loads the slip map
    /// </summary>
    public void LoadSlipMap()
    {
      IFormatter formatter = new BinaryFormatter();
      Stream stream = new FileStream($@"{SaveDir}/{FileName}", FileMode.Open, FileAccess.Read, FileShare.Read);
      try
      {
        Map = (SlipMap) formatter.Deserialize(stream);
      }
      catch (Exception)
      {
        // ignored
      }
      finally
      {
        stream.Close();
      }
    }

    /// <summary>
    /// This saves the slip map to a file.
    /// </summary>
    public void SaveSlipMap()
    {
      IFormatter formatter = new BinaryFormatter();
      if (!Directory.Exists(SaveDir))
        Directory.CreateDirectory(SaveDir);

        Stream stream = new FileStream($@"{SaveDir}\{FileName}", FileMode.OpenOrCreate,
          FileAccess.Write, FileShare.None);
        try
        {
          formatter.Serialize(stream, Map);
        }
        catch (NullReferenceException)
        {
          if (Map == null)
          {
            throw new Exception("You must create or load a slip map before you can save one.");
          }
          else
          {
            throw;
          }
        }
        finally
        {
          stream.Close();
        }

    }

    /// <summary>
    /// Returns the list of save files in the save directory.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> ListSectors()
    {
      if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
      var saveDir = new DirectoryInfo(SaveDir);
      var files = saveDir.GetFiles("*.sm"); // This retrieves a list of files with the extintion .sm
      return files.Select(file => file.Name);
    }

    /// <summary>
    /// This overrides the current s
    /// </summary>
    /// <param name="systemID"></param>
    public void OverrideCurrentSystem(int systemID)
    {
      Map.CurrentSystem = new StarSystem {ID = systemID};
    }

    /// <summary>
    ///   Find a route to the destination via a breadth first search.
    /// </summary>
    /// <param name="finalDestination"></param>
    /// <returns></returns>
    public List<StarSystem> FindRoute(int finalDestination)
    {
      if (CurrentSystem.ID==finalDestination) return new List<StarSystem>();
      var ignoreSystems = new List<StarSystem>();
      var possibleRoutes = new Queue<RouteNode>();
      possibleRoutes.Enqueue(new RouteNode {StarSystem = CurrentSystem});
      RouteNode current = null;
      while (possibleRoutes.Count > 0)
      {
        current = possibleRoutes.Dequeue();
        ignoreSystems.Add(current.StarSystem);
        if (current.StarSystem.ID == finalDestination)
        {
          break;
        }
        foreach (var connectedSystem in current.StarSystem.ConnectedSystems
          .Where(system => !ignoreSystems.Contains(system)))
        {
          possibleRoutes.Enqueue(
            new RouteNode
            {
              StarSystem = connectedSystem,
              ParentNode = current
            });
        }
      }
      var route = new List<StarSystem>();
      while (current != null && current.StarSystem != CurrentSystem)
      {
        route.Add(current.StarSystem);
        current = current.ParentNode;
      }
      if(route.Any(system=>system.ID==finalDestination))
      return route;
      else throw new Exception($"One does not simply walk to {VisitedSystems.First(system=>system.ID==finalDestination)}. No viable route exist to reach your destination... Blind Jump?");   
          
    }

    /// <summary>
    /// Organize SlipDrive Data.
    /// </summary>
    public void Clean()
    {
      Map.CleanAndOrganize();
    }
  }
}