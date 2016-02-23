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
    public class SlipDrive
    {
        public SlipDrive()
        {
            SaveDir = "SectorFiles";
        }

        public string FileName { get; set; }
        private string SaveDir { get; }
        private SlipMap Map { get; set; }


        public string NavigationJumpMessage { get; set; }
        public int LastSystemId => Map.LastSystemID;

        public StarSystem CurrentSystem => Map?.CurrentSystem;

        public IEnumerable<StarSystem> VisitedSystems => Map?.Systems;

        /// <summary>
        ///     Primarily used for skill rolls, this option allows you to see what was rolled
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
        ///     Primarily used for skill rolls, this option only allows you to see the result of what was rolled.
        /// </summary>
        /// <returns></returns>
        private static int DiceRoll3D6()
        {
            int a, b, c;
            return DiceRoll3D6(out a, out b, out c);
        }


        public void BlindJump(bool computer, int pilotSkillLevel)
        {
            int die1, die2, die3;
            var skillRoll = DiceRoll3D6(out die1, out die2, out die3);
            NavigationJumpMessage =
                $"Rolled {skillRoll} Vs. {pilotSkillLevel} with the following dice {die1}, {die2}, {die3}";
            if (skillRoll == 18 || skillRoll - pilotSkillLevel > 9)
            {
                NavigationJumpMessage = "Critical Failure: " + NavigationJumpMessage +
                                        "; Unfortunately with this roll you did not go anywhere. You now have a broken Grav lens and can not enter the slipstream untill it is repaired";
            }
            else if (skillRoll == 17)
            {
                NavigationJumpMessage = "Automatic Failure: " + NavigationJumpMessage +
                                        "; Unfortunately with this roll you did not go anywhere as the slip portal failed to open, Please Try Again";
            }
            else
                BlindJump(computer);
            var timeRoll = DiceRoll3D6();
            NavigationJumpMessage += $"\n It took {timeRoll - (pilotSkillLevel - skillRoll)} hours";
        }

        private void BlindJump(bool computer)
        {
            while (true)
            {
                int destinationId;
                StarSystem destination;
                var origin = Map.CurrentSystem;
                //If a computer makes a blind jump there is no attraction to known systems or a 2/3 chance for an organic
                var possibilities = new Random();
                if (computer || possibilities.Next(0, 3) != 0)
                {
                    destinationId = possibilities.Next(0, Map.LastSystemID + 1);
                    destination = Map.Systems.FirstOrDefault(s => s.ID == destinationId) ?? new StarSystem
                    {
                        ID = destinationId
                    };
                }
                else
                {
                    // If in that 1/3 chance chose a system out of known systems.
                    destination = Map.Systems.ElementAt(possibilities.Next(0, Map.Systems.Count));
                    if (destination.ID == origin.ID || destination.ConnectedSystems.Any(s => s.ID == origin.ID))
                    {
                        // Blind jumps are used to create new connections so jump again of a connection already exist or if origin is same as the destination.
                        continue;
                    }
                }
                CreateSlipRoute(origin, destination);
                Map.CurrentSystem = destination;
                break;
            }
            SaveSlipMap();
        }

        public void CreateSlipRoute(StarSystem origin, StarSystem destination)
        {
            if (origin.ConnectedSystems.Contains(destination) || destination.ConnectedSystems.Contains(origin))
                throw new Exception("Slip Route already exist.");
            origin.ConnectedSystems.Add(destination);
            destination.ConnectedSystems.Add(origin);
        }

        /// <summary>
        ///     Navigation jumps are used to follow known routes to a desired destination.
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
                if (possibilities.Next(0, 2) == 0)
                {
                    BlindJump(true);
                }
                else
                    Map.CurrentSystem = destination;
                return;
            }

            //If a biological organizim pilots the slipstream then things start getting complicated.
            //Roll for Pilot Skill
            int die1, die2, die3;
            var sum = DiceRoll3D6(out die1, out die2, out die3);
            NavigationJumpMessage = $"Rolled {sum} Vs. {pilotSkillLevel} with the following dice {die1}, {die2}, {die3}";

            // Check for critical success, where Critical success is a 3 or success by 10 or more.
            if (sum == 3 || pilotSkillLevel - sum > 9)
            {
                Map.CurrentSystem = destination;
                NavigationJumpMessage = "Critical Success: " + NavigationJumpMessage;
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
                if (possibilities.Next(0, 100) < 95)
                {
                    Map.CurrentSystem = destination;
                    NavigationJumpMessage = "Automatic Success: " + NavigationJumpMessage;
                }
                else
                {
                    BlindJump(false);
                    NavigationJumpMessage = "Automatic Success: " + NavigationJumpMessage + "; Unfortunately 5% of all " +
                                            "Automatic Successes result in a blind jump.";
                }
            }

            //Check for successs
            else if (sum < pilotSkillLevel)
            {
                if (possibilities.Next(0, 100) < 90)
                {
                    Map.CurrentSystem = destination;
                    NavigationJumpMessage = "Success: " + NavigationJumpMessage;
                }
                else
                {
                    BlindJump(false);
                    NavigationJumpMessage = "Success: " + NavigationJumpMessage + "; Unfortunately 10% of all Normal " +
                                            "Successes result in a blind jump.";
                }
            }

            //check for Failure
            else if (sum > pilotSkillLevel)
            {
                if (possibilities.Next(0, 100) < 50)
                {
                    Map.CurrentSystem = destination;
                    NavigationJumpMessage = "Failure: " + NavigationJumpMessage +
                                            "; Dispite this failure you managed to find your way with the same chances as a ship AI";
                }
                else
                {
                    BlindJump(false);
                    NavigationJumpMessage = "Success: " + NavigationJumpMessage +
                                            "; Unfortunately 50% of all normal failures result in a blind jump.";
                }
            }
            else if (sum == 17)
            {
                NavigationJumpMessage = "Automatic Failure: " + NavigationJumpMessage +
                                        "; Unfortunately with this roll you did not go anywhere as the slip portal failed to open, Please Try Again";
            }
        }

        /// <summary>
        ///     Create a slip map, specify the number of systems and the current system ID is optional. if current system is left
        ///     null then current system will be randomly assigned
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

        public void SaveSlipMap()
        {
            IFormatter formatter = new BinaryFormatter();
            if (!Directory.Exists(SaveDir))
                Directory.CreateDirectory(SaveDir);
            try
            {
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
            catch (Exception)
            {
                // ignored
            }
        }

        public IEnumerable<string> ListSectors()
        {
            if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
            var saveDir = new DirectoryInfo(SaveDir);
            var files = saveDir.GetFiles("*.sm"); // This retrieves a list of files with the extintion .sm
            return files.Select(file => file.Name);
        }

        public void OverrideCurrentSystem(int parse)
        {
            Map.CurrentSystem = new StarSystem {ID = parse};
        }

        /// <summary>
        /// Find a route to the destination via a breadth first search.
        /// </summary>
        /// <param name="finalDestination"></param>
        /// <returns></returns>
        public List<StarSystem> FindRoute(int finalDestination)
        {
            var ignoreSystems = new List<int>();
            var possibleRoutes = new Queue<RouteNode>();
            possibleRoutes.Enqueue(new RouteNode {StarSystem = CurrentSystem});
            RouteNode current = null;
            while (possibleRoutes.Count > 0)
            {
                current = possibleRoutes.Dequeue();
                ignoreSystems.Add(current.StarSystem.ID);
                if (current.StarSystem.ID == finalDestination)
                {
                    break;
                }
                foreach (var connectedSystem in current.StarSystem.ConnectedSystems
                    .Where(system=>!ignoreSystems.Contains(system.ID)))
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
            while (current != null)
            {
                route.Add(current.StarSystem);
                current = current.ParentNode;
            }
            return route;
        }

        public void Clean()
        {
            Map.Clean();
        }
    }

    public class RouteNode
    {
        public RouteNode ParentNode { get; set; }
        public StarSystem StarSystem { get; set; }
    }
}