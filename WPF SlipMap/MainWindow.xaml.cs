// Slipstream Map WPF SlipMap MainWindow.xaml.cs
// Created: 2016-02-24 4:17 PM
// Last Edited: 2016-03-04 12:23 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SlipMap_Code_Library;

#endregion

namespace WPF_SlipMap
{
   /// <summary>
   ///    Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow
   {
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
            SlipDrive.CreateSlipRoute(CreateSlipRouteOrigin.SelectedValue as StarSystem,
               CreateSlipRouteDestination.SelectedValue as StarSystem);
            RefreshCurrentSystem();
            Notification.Text = "Slip Route Added";
            Notification.Foreground = Brushes.LawnGreen;
         }
         catch (Exception exception)
         {
            Notification.Text = exception.Message;
            Notification.Foreground = Brushes.Red;
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
               Notification.Text = error.Message;
               Notification.Foreground = Brushes.Red;
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
            Notification.Foreground = Brushes.LawnGreen;
            Notification.Text = "Updated Current System";
         }
         catch (Exception error)
         {
            Notification.Foreground = Brushes.Red;
            Notification.Text = error.Message;
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
         Notification.Foreground=Brushes.Red;
         Notification.Text = "Set pilot skill and try again.";
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
      private void SaveSlipMap()
      {
         try
         {
            SlipDrive.SaveSlipMap();
         }
         catch (Exception error)
         {
            Notification.Text= $"There was an error saving the Slip Map. \n {error.Message}";
            Notification.Foreground = Brushes.Red;
         }
      }


      private void CleanUp_Click(object sender, RoutedEventArgs e)
      {
         SlipDrive.Clean();
         Notification.Text = "The Slip map has been organized.";
         Notification.Foreground = Brushes.LawnGreen;
         Refresh();
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
            Notification.Foreground=Brushes.Red;
            Notification.Text = error.Message;
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
            Notification.Foreground = Brushes.Yellow;
            Notification.Text = SlipDrive.NavigationJumpMessage;
         }
         SlipDrive.NavigationJumpMessage = string.Empty;
      }

      #endregion
   }
}