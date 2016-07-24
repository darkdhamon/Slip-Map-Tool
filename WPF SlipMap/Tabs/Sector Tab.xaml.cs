// Slipstream Map WPF SlipMap Sector Tab.xaml.cs
// Created: 2016-03-03 12:58 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SlipMap_Code_Library;
using WPF_SlipMap.Application;

#endregion

namespace WPF_SlipMap.Tabs
{
   /// <summary>
   ///    Interaction logic for Sector_Tab.xaml
   /// </summary>
   public partial class SectorTab
   {
       private SlipDrive _slipDrive;

       public SectorTab()
      {
         InitializeComponent();
            
      }

       public SlipDrive SlipDrive
       {
           private get { return _slipDrive; }
           set
           {
               _slipDrive = value;
               UpdateView();
           }
       }

       private void UpdateView()
       {
            SectorName.Text = Sectors.Text = SlipDrive.FileName;
       }

       public Session Session { get; set; }
      public MainWindow MainWindow { private get; set; }

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
               MainWindow.Notify("Numeric Values only", NoteType.Failure);
               return;
            }
            MainWindow.Notify("New Sector has been created!", NoteType.Success);
         }
         catch (Exception error)
         {
            MainWindow.Notify(error.Message,NoteType.Failure);
         }
          SectorName.Text = SlipDrive.FileName;
         MainWindow.Refresh();
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
          SectorName.Text = SlipDrive.FileName;
         MainWindow.Refresh();
            MainWindow.Notify($"{SlipDrive.FileName} has been loaded",NoteType.Success);
      }

      private void Expander_OnExpanded(object sender, RoutedEventArgs e)
      {
         Sectors.ItemsSource = SlipDrive.ListSectors();
      }

       private void SaveSector_OnClick(object sender, RoutedEventArgs e)
       {
           if (!SlipDrive.FileName.Equals(SectorName.Text) && !string.IsNullOrWhiteSpace(SectorName.Text))
           {
               SlipDrive.FileName = SectorName.Text.EndsWith(".sm") ? SectorName.Text : $"{SectorName.Text}.sm";
           }
           MainWindow.SaveSlipMap();
       }
   }
}