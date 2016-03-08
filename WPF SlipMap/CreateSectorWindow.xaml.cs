// Slipstream Map WPF SlipMap CreateSectorWindow.xaml.cs
// Created: 2015-12-07 10:00 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Windows;
using System.Windows.Controls;

#endregion

namespace WPF_SlipMap
{
   /// <summary>
   ///    Interaction logic for CreateSectorWindow.xaml
   /// </summary>
   public partial class CreateSectorWindow : Window
   {
      private readonly MainWindow _mainWindow;

      public CreateSectorWindow(MainWindow mainWindow)
      {
         _mainWindow = mainWindow;
         LastSystemID = -1;
         InitializeComponent();
      }

      private int LastSystemID { get; set; }

      private int CurrentSystemID { get; set; }


      private void LastSystem_TextChanged(object sender, TextChangedEventArgs e)
      {
         try
         {
            int systemID;
            if (int.TryParse(((TextBox) sender).Text, out systemID) && (systemID > 0))
            {
               LastSystemID = systemID;
               if (LastSystemError != null) LastSystemError.Text = null;
            }
            else
            {
               throw new InvalidInputException("Needs to be a valid number greater than 0");
            }
         }
         catch (Exception error)
         {
            if (LastSystemError != null) LastSystemError.Text = error.Message;
         }
      }

      private void CheckBox_Checked(object sender, RoutedEventArgs e)
      {
         if (StartingSystem != null) StartingSystem.Visibility = Visibility.Collapsed;
      }

      private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
      {
         if (StartingSystem != null) StartingSystem.Visibility = Visibility.Visible;
      }

      private void SystemID_TextChanged(object sender, TextChangedEventArgs e)
      {
         try
         {
            int systemID;
            if (int.TryParse(((TextBox) sender).Text, out systemID) && systemID > 0 && systemID <= LastSystemID)
            {
               CurrentSystemID = systemID;
               if (CurrentSystemError != null) CurrentSystemError.Text = null;
            }
            else
            {
               throw new InvalidInputException("Needs to be a valid number greater than -1 and less than " +
                                               (LastSystemID + 1));
            }
         }
         catch (Exception error)
         {
            if (CurrentSystemError != null) CurrentSystemError.Text = error.Message;
         }
      }

      private void Create_Click(object sender, RoutedEventArgs e)
      {
         _mainWindow.SlipDrive.FileName = SectorName.Text + ".sm";
         if (RandomStartEnabled.IsChecked == true)
            _mainWindow.SlipDrive.CreateSlipMap(LastSystemID);
         else _mainWindow.SlipDrive.CreateSlipMap(LastSystemID, CurrentSystemID);
         _mainWindow.Refresh();
         Close();
      }
   }
}