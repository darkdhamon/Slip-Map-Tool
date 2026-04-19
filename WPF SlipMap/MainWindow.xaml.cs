// Slipstream Map WPF SlipMap MainWindow.xaml.cs
// Created: 2016-02-24 4:17 PM
// Last Edited: 2016-03-04 12:23 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SlipMap_Code_Library;
using WPF_SlipMap.Application;
using WPF_SlipMap.Tabs;

#endregion

namespace WPF_SlipMap
{
   /// <summary>
   ///    Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow
   {
      public const string ExportAllSectorsOption = "All Sectors";

      #region Library Tab

      /// <summary>
      ///    Display System Notes when System Name is selected in the Library
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void LibrarySystemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (LibrarySystemsList.SelectedItem != null)
            LibrarySystemNotes.Text = ((StarSystem) LibrarySystemsList.SelectedItem).Notes;
      }

      #endregion

      #region Navigation Tab

      /// <summary>
      ///    Plot and Diplay Navigation
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void PlotDestinationSystem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         DestinationSystem = PlotDestinationSystem.SelectedValue as StarSystem;
         DisplayRoute();
      }

      #endregion

      #region Feilds And Properties

      /// <summary>
      ///    This is a backing field for Slipdrive
      /// </summary>
      private SlipDrive _slipDrive;

      private Session _session;

      /// <summary>
      ///    Return Slip Drive (AutoInstantiated)
      /// </summary>
      public SlipDrive SlipDrive => _slipDrive ?? (_slipDrive = new SlipDrive());

      /// <summary>
      ///    This is the name of the Session of the file.
      /// </summary>
      private static string SessionFile => "LastSession.session";

      /// <summary>
      ///    The is the skill of the Pilot.
      /// </summary>
      private int PilotSkillLevel => Session.PilotSkill;

      public Session Session
      {
         get { return _session ?? (_session = new Session()); }
         set { _session = value; }
      }

      /// <summary>
      ///    Shortcut to get Current System from SlipDrive
      /// </summary>
      private StarSystem CurrentSystem => SlipDrive.CurrentSystem;

      /// <summary>
      /// </summary>
      private StarSystem DestinationSystem { get; set; }

      #endregion

      #region Initialization and Closing

      public MainWindow()
      {
         InitializeComponent();
         LoadLastSession();
         SettingsTab.Session = Session;
         SettingsTab.MainWindow = this;
         SectorTab.Session = Session;
         SectorTab.SlipDrive = SlipDrive;
         SectorTab.MainWindow = this;

      }

      /// <summary>
      ///    Close Events
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void MainWindow_OnClosing(object sender, CancelEventArgs e)
      {
         SaveSlipMap();
         SaveSession();
      }

      /// <summary>
      ///    Loads the last session
      /// </summary>
      private void LoadLastSession()
      {
         if (!File.Exists(SessionFile)) return; // if last session does not exist ignore the rest
         IFormatter formatter = new BinaryFormatter();
         Stream stream = new FileStream(SessionFile, FileMode.Open, FileAccess.Read, FileShare.Read);
         try
         {
            Session = (Session) formatter.Deserialize(stream);
            // Reload settings from last session.
            // Load Last accessed Secore from File
            SlipDrive.FileName = Session.FileName;
            SlipDrive.LoadSlipMap();

            // Load Destination From last Session
            DestinationSystem = Session.Destination;
         }
         catch
         {
            // ignored
         }
         finally
         {
            stream.Close();
            Refresh();
         }
      }

      #endregion

      #region GM Overrides

      /// <summary>
      ///    Create a slip route between two known star systems.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void CreateSlipRoute_OnClick(object sender, RoutedEventArgs e)
      {
         try
         {
             var origin = CreateSlipRouteOrigin.SelectedValue as StarSystem;
             var dest = CreateSlipRouteDestination.SelectedValue as StarSystem;
            SlipDrive.CreateSlipRoute(origin,
               dest);
            RefreshCurrentSystem();
           Notify($"Slip Route between {origin} and {dest} has been created.", NoteType.Success);
         }
         catch (Exception exception)
         {
            Notify(exception.Message, NoteType.Failure);
         }
      }

      /// <summary>
      ///    Validate System ID when text is changed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void SystemOverride_OnTextChanged_TextChanged(object sender, TextChangedEventArgs e)
      {
         try
         {
            int overrideSystemId;
            if (Notification != null) Notification.Text = null;
            if (!int.TryParse(((TextBox) sender).Text, out overrideSystemId) &&
                (overrideSystemId < 0 || overrideSystemId > SlipDrive.LastSystemId))
               throw new InvalidInputException("This must be a valid number");
         }
         catch (InvalidInputException error)
         {
            if (Notification != null)
            {
               Notify(error.Message,NoteType.Failure);
            }
         }
      }


      /// <summary>
      ///    Override Current System
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OverrideCurrentSystem_Click(object sender, RoutedEventArgs e)
      {
         try
         {
            SlipDrive.OverrideCurrentSystem(int.Parse(SystemOverride.Text));
            Refresh();
            Notify($"Current system set to {SlipDrive.CurrentSystem}",NoteType.Success);
         }
         catch (Exception error)
         {
            Notify(error.Message,NoteType.Failure);
         }
      }

      #endregion

      #region Current System Tab Actions

      /// <summary>
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void NavJump_Click(object sender, RoutedEventArgs e)
      {
         if (IsPilotSkillSet)
         {
            SlipDrive.NavigationJump(false, (StarSystem) Routes.SelectedItem, PilotSkillLevel);
            Refresh();
         }
         else
         {
            RequestPilotSkill();
         }
      }

      private bool IsPilotSkillSet => !SettingsTab.PilotSkill.Text.Equals("Unset");

      private void RequestPilotSkill()
      {
         Notify("Please set pilot skill.");
         var tabControl = SettingsTab.Parent as TabControl;
         if (tabControl != null) tabControl.SelectedItem = SettingsTab;
         SettingsTab.PilotSkill.Focus();
      }

      /// <summary>
      ///    Blind Jump
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void BlindJump_Click(object sender, RoutedEventArgs e)
      {
         if (IsPilotSkillSet)
         {
            SlipDrive.BlindJump(false, PilotSkillLevel);
            Refresh();
         }
         else
         {
            RequestPilotSkill();
         }
      }

      /// <summary>
      ///    Update Current System Name
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void SysName_TextChanged(object sender, TextChangedEventArgs e)
      {
         if (CurrentSystem != null) CurrentSystem.Name = SysName.Text;
      }

      /// <summary>
      ///    Update GM Notes for Current System
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void GMNotes_TextChanged(object sender, TextChangedEventArgs e)
      {
         if (CurrentSystem != null) CurrentSystem.Notes = GmNotes.Text;
      }

      #endregion

      #region Save and Maintenance Actions

      /// <summary>
      ///    Save the current session
      /// </summary>
      private void SaveSession()
      {
         IFormatter formatter = new BinaryFormatter();
         Stream stream = new FileStream(SessionFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
         Session.FileName = SlipDrive.FileName;
         Session.PilotSkill = PilotSkillLevel;
         Session.Destination = DestinationSystem;
         try
         {
            formatter.Serialize(stream, Session);
         }
         catch (Exception error)
         {
            MessageBox.Show("There was an error saving your session. \n" + error.Message, "Save Error",
               MessageBoxButton.OK, MessageBoxImage.Error);
         }
         finally
         {
            stream.Close();
         }
      }

      /// <summary>
      ///    Save the SlipMap
      /// </summary>
      public void SaveSlipMap()
      {
         try
         {
            SlipDrive.SaveSlipMap();
                Notify($"Manually saved {SlipDrive.FileName}");
         }
         catch (Exception error)
         {
            Notify($"There was an error saving the Slip Map. \n {error.Message}", NoteType.Failure);
         }
      }


      private void CleanUp_Click(object sender, RoutedEventArgs e)
      {
         SlipDrive.Clean();
         Notify("The Slip Map has been organised.", NoteType.Success);
         Refresh();
      }

      public void ExportSlipMapToJson(string selectedSectorFileName)
      {
         try
         {
            var sectorFiles = SlipDrive.ListSectors().ToList();
            if (!sectorFiles.Any())
            {
               Notify("No sector save files were found to export.", NoteType.Failure);
               return;
            }

            var exportAllSectors = string.IsNullOrWhiteSpace(selectedSectorFileName) ||
                                   selectedSectorFileName.Equals(ExportAllSectorsOption, StringComparison.OrdinalIgnoreCase);
            var selectedSectorFiles = exportAllSectors
               ? sectorFiles
               : sectorFiles
                  .Where(fileName => fileName.Equals(selectedSectorFileName, StringComparison.OrdinalIgnoreCase))
                  .ToList();

            if (!selectedSectorFiles.Any())
            {
               Notify("Select a valid sector save file to export.", NoteType.Failure);
               return;
            }

            var exportDirectory = SlipDrive.SaveDirectory;
            Directory.CreateDirectory(exportDirectory);

            var exportDialog = new SaveFileDialog
            {
               AddExtension = true,
               DefaultExt = ".json",
               Filter = "JSON Slip Map (*.json)|*.json|All Files (*.*)|*.*",
               FileName = exportAllSectors
                  ? "SlipMap-Legacy-Export.json"
                  : $"{Path.GetFileNameWithoutExtension(selectedSectorFiles.Single())}.json",
               InitialDirectory = exportDirectory,
               OverwritePrompt = true,
               Title = exportAllSectors
                  ? "Export All Legacy Slip Map Saves For New App"
                  : "Export Legacy Slip Map Save For New App"
            };

            if (exportDialog.ShowDialog(this) != true)
            {
               return;
            }

            var json = BuildSlipMapJson(selectedSectorFiles);
            File.WriteAllText(exportDialog.FileName, json);
            Notify($"Exported {selectedSectorFiles.Count} save file(s) to JSON for the new app: {exportDialog.FileName}", NoteType.Success);
         }
         catch (Exception error)
         {
            Notify($"There was an error exporting JSON. \n {error.Message}", NoteType.Failure);
         }
      }

      private string BuildSlipMapJson(List<string> sectorFiles)
      {
         var originalFileName = SlipDrive.FileName;
         var originalDestinationSystem = DestinationSystem;
         var maps = new List<SlipMapJsonDocument>();

         foreach (var sectorFile in sectorFiles)
         {
            SlipDrive.FileName = sectorFile;
            SlipDrive.LoadSlipMap();
            if (CurrentSystem == null)
            {
               continue;
            }

            maps.Add(BuildCurrentSlipMapJsonDocument(sectorFile));
         }

         if (!string.IsNullOrWhiteSpace(originalFileName))
         {
            SlipDrive.FileName = originalFileName;
            SlipDrive.LoadSlipMap();
            DestinationSystem = originalDestinationSystem;
            Refresh();
         }

         var document = new LegacyExportJsonDocument
         {
            schemaVersion = 1,
            exportedAt = DateTime.Now,
            legacySession = BuildLegacySessionJsonDocument(),
            maps = maps
         };

         return new JavaScriptSerializer().Serialize(document);
      }

      private SlipMapJsonDocument BuildCurrentSlipMapJsonDocument(string sectorFileName)
      {
         var visitedSystems = SlipDrive.VisitedSystems?.ToList() ?? new List<StarSystem>();
         var routeKeys = new HashSet<string>();
         var routes = new List<SlipRouteJsonDocument>();

         foreach (var system in visitedSystems.Where(system => system != null))
         {
            foreach (var connectedSystem in system.ConnectedSystems.Where(connectedSystem => connectedSystem != null))
            {
               if (system.ID == connectedSystem.ID)
               {
                  continue;
               }

               var firstSystemId = Math.Min(system.ID, connectedSystem.ID);
               var secondSystemId = Math.Max(system.ID, connectedSystem.ID);
               if (!routeKeys.Add($"{firstSystemId}:{secondSystemId}"))
               {
                  continue;
               }

               routes.Add(new SlipRouteJsonDocument
               {
                  firstSystemId = firstSystemId,
                  secondSystemId = secondSystemId
               });
            }
         }

         routes = routes
            .OrderBy(route => route.firstSystemId)
            .ThenBy(route => route.secondSystemId)
            .ToList();

         return new SlipMapJsonDocument
         {
            schemaVersion = 1,
            lastSystemId = SlipDrive.LastSystemId,
            currentSystemId = CurrentSystem.ID,
            sectorFileName = string.IsNullOrWhiteSpace(sectorFileName) ? null : sectorFileName,
            starSystems = visitedSystems
               .OrderBy(system => system.ID)
               .Select(system => new StarSystemJsonDocument
               {
                  id = system.ID,
                  name = string.IsNullOrWhiteSpace(system.Name) ? null : system.Name,
                  notes = string.IsNullOrWhiteSpace(system.Notes) ? null : system.Notes
               })
               .ToList(),
            routes = routes
         };
      }

      private LegacySessionJsonDocument BuildLegacySessionJsonDocument()
      {
         return new LegacySessionJsonDocument
         {
            displayName = string.IsNullOrWhiteSpace(Session.DisplayName) ? null : Session.DisplayName,
            pilotSkill = Session.PilotSkill,
            sectorFileName = string.IsNullOrWhiteSpace(Session.FileName) ? null : Session.FileName,
            destinationSystemId = Session.Destination?.ID
         };
      }

      #endregion

      #region refresh methods

      /// <summary>
      ///    Refreshes UI with updated data
      /// </summary>
      public void Refresh()
      {
         // if currentsystem is null then there is no sector loaded and this logic does no good.
         if (CurrentSystem == null) return;

          Title = $"{SlipDrive.FileName.Replace(".sm","")} Sector Slip Map";

         RefreshCurrentSystem();
         RefreshSystemLibrary();
         DisplayJumpMessage();
         RefreshNavigation();

         RefreshGMOverrides();
      }

      /// <summary>
      ///    Refresh GM Override Tab.
      /// </summary>
      private void RefreshGMOverrides()
      {
         CreateSlipRouteOrigin.Items.Clear();
         foreach (var system in SlipDrive.VisitedSystems)
         {
            CreateSlipRouteOrigin.Items.Add(system);
         }

         CreateSlipRouteDestination.Items.Clear();
         foreach (var system in SlipDrive.VisitedSystems)
         {
            CreateSlipRouteDestination.Items.Add(system);
         }
         CreateSlipRouteDestination.SelectedValue = DestinationSystem;
      }

      /// <summary>
      ///    Refresh the Navigation Tab
      /// </summary>
      private void RefreshNavigation()
      {
         PlotCurrentSystem.Content = CurrentSystem;
         PlotDestinationSystem.Items.Clear();
         foreach (var system in SlipDrive.VisitedSystems)
         {
            PlotDestinationSystem.Items.Add(system);
         }
         PlotDestinationSystem.SelectedItem = DestinationSystem;
         DisplayRoute();
      }

      /// <summary>
      /// Shows the route.
      /// </summary>
      private void DisplayRoute()
      {
         try
         {
            if (DestinationSystem == null) return;
            var route = SlipDrive.FindRoute(DestinationSystem.ID);
            NumberOfJumps.Content = route.Count;
            RouteList.Items.Clear();

            route.Reverse();
            foreach (var system in route)
            {
               RouteList.Items.Add(system);
            }
         }
         catch (Exception error)
         {
            DestinationSystem = null;
            PlotDestinationSystem.SelectedValue = null;
            Notify(error.Message, NoteType.Failure);
         }
      }


      /// <summary>
      ///    Refresh Library Tab
      /// </summary>
      private void RefreshSystemLibrary()
      {
         LibrarySystemsList.Items.Clear();
         foreach (var connectedSystem in SlipDrive.VisitedSystems)
         {
            LibrarySystemsList.Items.Add(connectedSystem);
         }
      }

      /// <summary>
      ///    Refresh Current System Tab
      /// </summary>
      private void RefreshCurrentSystem()
      {
         SysId.Content = $"{CurrentSystem.ID}:";
         SysName.Text = CurrentSystem.Name;
         GmNotes.Text = CurrentSystem.Notes;
         Routes.Items.Clear();
         foreach (var connectedSystem in CurrentSystem.ConnectedSystems)
         {
            Routes.Items.Add(connectedSystem);
         }
      }

      /// <summary>
      ///    Display Jump Message Notification
      /// </summary>
      private void DisplayJumpMessage()
      {
         if (!string.IsNullOrWhiteSpace(SlipDrive.NavigationJumpMessage))
         {
            Notify(SlipDrive.NavigationJumpMessage);
         }
         SlipDrive.NavigationJumpMessage = string.Empty;
      }

      #endregion

       public void Notify(string msg, NoteType noteType = NoteType.Information)
       {
           Notification.Text = $"{msg}\nLast Save: {SlipDrive.LastSave}";
           switch (noteType)
           {
               case NoteType.Success:
                    Notification.Foreground = Brushes.LawnGreen;
                    break;
               case NoteType.Failure:
                    Notification.Foreground = Brushes.Red;
                    break;
               case NoteType.Information:
                    Notification.Foreground = Brushes.Yellow;
                    break;
                    
               default:
                   throw new ArgumentOutOfRangeException(nameof(noteType), noteType, null);
           }
       }

       private sealed class LegacyExportJsonDocument
       {
          public int schemaVersion { get; set; }
          public DateTime exportedAt { get; set; }
          public LegacySessionJsonDocument legacySession { get; set; }
          public List<SlipMapJsonDocument> maps { get; set; }
       }

       private sealed class SlipMapJsonDocument
       {
          public int schemaVersion { get; set; }
          public string sectorFileName { get; set; }
          public int lastSystemId { get; set; }
          public int currentSystemId { get; set; }
          public List<StarSystemJsonDocument> starSystems { get; set; }
          public List<SlipRouteJsonDocument> routes { get; set; }
       }

       private sealed class LegacySessionJsonDocument
       {
          public string displayName { get; set; }
          public int pilotSkill { get; set; }
          public string sectorFileName { get; set; }
          public int? destinationSystemId { get; set; }
       }

       private sealed class StarSystemJsonDocument
       {
          public int id { get; set; }
          public string name { get; set; }
          public string notes { get; set; }
       }

       private sealed class SlipRouteJsonDocument
       {
          public int firstSystemId { get; set; }
          public int secondSystemId { get; set; }
       }
   }
}
