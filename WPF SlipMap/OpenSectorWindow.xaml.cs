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
    /// Interaction logic for OpenSectorWindow.xaml
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
