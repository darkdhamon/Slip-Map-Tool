// Slipstream Map WPF SlipMap Settings Tab.xaml.cs
// Created: 2016-03-03 12:57 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace WPF_SlipMap.Tabs
{
   /// <summary>
   ///    Interaction logic for Settings_Tab.xaml
   /// </summary>
   public partial class SettingsTab
   {
      public MainWindow MainWindow { get; set; }
      private Session _session;

      public SettingsTab()
      {
         InitializeComponent();
      }

      public Session Session
      {
         get { return _session; }
         set
         {
            _session = value;
            if (_session.PilotSkill != 0)
               PilotSkill.Text = _session.PilotSkill.ToString();
            DisplayName.Text = _session.DisplayName;
         }
      }

      #region Settings Actions

      /// <summary>
      ///    Update the Pilot Skill
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void PilotSkill_TextChanged(object sender, TextChangedEventArgs e)
      {
         try
         {
            int pilotSkill;
            if (!int.TryParse(((ComboBox) sender).Text, out pilotSkill))
               throw new InvalidInputException("This must be a valid number.");
            Session.PilotSkill = pilotSkill;
            MainWindow.Notification.Foreground = Brushes.LawnGreen;
            MainWindow.Notification.Text = "Pilot skill has been set.";
         }
         catch (InvalidInputException error)
         {
            MainWindow.Notification.Foreground = Brushes.Red;

            MainWindow.Notification.Text = error.Message;
         }
         catch (Exception error)
         {
            MainWindow.Notification.Foreground = Brushes.Red;
            MainWindow.Notification.Text = error.Message + error.GetType();
         }
      }

      #endregion

      private void DisplayName_TextChanged(object sender, TextChangedEventArgs e)
      {
         Session.DisplayName = DisplayName.Text;
      }
   }
}