using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPF_SlipMap
{
    /// <summary>
    /// Interaction logic for CreateSector.xaml
    /// </summary>
    public partial class CreateSector : Window
    {
        private readonly MainWindow _mainWindow;
        private int LastSystemID { get; set; }

        public CreateSector(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            LastSystemID = -1;
            InitializeComponent();
        }

        
        private void LastSystem_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int systemID;
                if (int.TryParse(((TextBox) sender).Text, out systemID)&&(systemID>0))
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
            if (StartingSystem != null) StartingSystem.Visibility=Visibility.Collapsed;
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
                if (int.TryParse(((TextBox)sender).Text, out systemID) && (systemID > 0&& systemID<=LastSystemID))
                {
                    CurrentSystemID = systemID;
                    if (CurrentSystemError != null) CurrentSystemError.Text = null;
                }
                else
                {
                    throw new InvalidInputException("Needs to be a valid number greater than -1 and less than " + (LastSystemID+1));
                }
            }
            catch (Exception error)
            {
                if (CurrentSystemError != null) CurrentSystemError.Text = error.Message;
            }
        }

        private int CurrentSystemID { get; set; }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.SlipDrive.FileName = SectorName.Text+".sm";
            if(RandomStartEnabled.IsChecked==true)
            _mainWindow.SlipDrive.CreateSlipMap(LastSystemID);
            else _mainWindow.SlipDrive.CreateSlipMap(LastSystemID, CurrentSystemID);
            _mainWindow.Refresh();
            Close();
        }
    }
}
