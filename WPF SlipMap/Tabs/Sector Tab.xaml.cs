// Slipstream Map WPF SlipMap Sector Tab.xaml.cs
// Created: 2016-03-03 12:58 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SlipMap_Code_Library;

#endregion

namespace WPF_SlipMap.Tabs
{
   /// <summary>
   ///    Interaction logic for Sector_Tab.xaml
   /// </summary>
   public partial class SectorTab
   {
      public SectorTab()
      {
         InitializeComponent();
      }

      public SlipDrive SlipDrive { get; set; }
      public Session Session { get; set; }
      public MainWindow MainWindow { get; set; }

      private void CreateSector_Click(object sender, RoutedEventArgs e)
      {
         try
         {
            int lastSystemID, startSystemID;
            SlipDrive.FileName = CreateSectorName.Text + ".sm";
            if (RandomSystem.IsChecked == true && int.TryParse(CreateLastID.Text, out lastSystemID))
               SlipDrive.CreateSlipMap(lastSystemID);
            else if (RandomSystem.IsChecked == false && int.TryParse(CreateLastID.Text, out lastSystemID) &&
                     int.TryParse(CreateStartID.Text, out startSystemID))
               SlipDrive.CreateSlipMap(lastSystemID, int.Parse(CreateStartID.Text));
            else
            {
               DisplaySectorResult("Numeric Values only");
               return;
            }
            DisplaySectorResult("New Sector has been created!", true);
         }
         catch (Exception error)
         {
            DisplaySectorResult(error.Message);
         }
         MainWindow.Refresh();
      }

      private void DisplaySectorResult(string message, bool success = false)
      {
         MainWindow.Notification.Text = message;
         switch (success)
         {
            case true:
               MainWindow.Notification.Foreground = Brushes.LawnGreen;
               break;
            default:
               MainWindow.Notification.Foreground = Brushes.Red;
               break;
         }
      }

      private void RandomSystem_Checked(object sender, RoutedEventArgs e)
      {
         SetSystem.Visibility = Visibility.Collapsed;
      }

      private void RandomSystem_OnUnchecked(object sender, RoutedEventArgs e)
      {
        SetSystem.Visibility = Visibility.Visible;
      }

      private void LoadSector_OnClick(object sender, RoutedEventArgs e)
      {
         SlipDrive.FileName = Sectors.SelectedItem.ToString();
         SlipDrive.LoadSlipMap();
         MainWindow.Refresh();
         MainWindow.Notification.Foreground = Brushes.LawnGreen;
         MainWindow.Notification.Text = "The Sector has been loaded";
      }

      private void Expander_OnExpanded(object sender, RoutedEventArgs e)
      {
         Sectors.ItemsSource = SlipDrive.ListSectors();
      }
   }
}