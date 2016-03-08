// Slipstream Map WPF SlipMap OpenSectorWindow.xaml.cs
// Created: 2015-12-07 9:41 PM
// Last Edited: 2016-03-04 12:14 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System.Windows;

#endregion

namespace WPF_SlipMap
{
   /// <summary>
   ///    Interaction logic for OpenSectorWindow.xaml
   /// </summary>
   public partial class OpenSectorWindow : Window
   {
      private readonly MainWindow _mainWindow;

      public OpenSectorWindow(MainWindow mainWindow)
      {
         _mainWindow = mainWindow;
         InitializeComponent();
         Sectors.ItemsSource = _mainWindow.SlipDrive.ListSectors();
      }

      private void Open_Click(object sender, RoutedEventArgs e)
      {
         _mainWindow.SlipDrive.FileName = Sectors.SelectedItem.ToString();
         _mainWindow.SlipDrive.LoadSlipMap();
         _mainWindow.Refresh();
         Close();
      }
   }
}