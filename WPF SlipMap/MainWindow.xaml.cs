using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using SlipMap_Code_Library;

namespace WPF_SlipMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SlipDrive _slipDrive;

        public MainWindow()
        {
            InitializeComponent();
            //MessageBox.Show("Remember: Set Pilot Skill");
            IFormatter formatter = new BinaryFormatter();
            if (!File.Exists(SessionFile)) return;
            Stream stream = new FileStream(SessionFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var session = (LastSession) formatter.Deserialize(stream);
                PilotSkill.Text = (PilotSkillLevel = session.PilotSkill).ToString();
                SlipDrive.FileName = session.FileName;
                SlipDrive.LoadSlipMap();
                Refresh();
            }
            catch
            {
                // ignored
            }
            finally
            {
                stream.Close();
            }
        }

        private static string SessionFile => "LastSession.session";

        public SlipDrive SlipDrive => _slipDrive??(_slipDrive = new SlipDrive());

        private int PilotSkillLevel { get; set; }

        private void PilotSkill_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (PilotError != null) PilotError.Text = null;
                int pilotSkill;
                if (!int.TryParse(((ComboBox) sender).Text, out pilotSkill))
                    throw new InvalidInputException("This must be a valid number");
                PilotSkillLevel = pilotSkill;
            }
            catch (InvalidInputException error)
            {
                if (PilotError != null) PilotError.Text = error.Message;
            }
            catch (Exception error)
            {
                if (PilotError != null) PilotError.Text = error.Message + error.GetType();
            }
        }

        private void SystemOverride_OnTextChanged_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int overrideSystemId;
                if (SystemError != null) PilotError.Text = null;
                if (!int.TryParse(((ComboBox)sender).Text, out overrideSystemId)&&(overrideSystemId<0||overrideSystemId>SlipDrive.LastSystemId))
                    throw new InvalidInputException("This must be a valid number");
            }
            catch (InvalidInputException error)
            {
                if (SystemError != null) PilotError.Text = error.Message;
            }
            catch (Exception error)
            {
                if (SystemError != null) PilotError.Text = error.Message + error.GetType();
            }
        }

        private void NewSector_OnClick(object sender, RoutedEventArgs e)
        {
            var cs = new CreateSector(this);
            cs.Show();
        }

        private void OpenSector_OnClick(object sender, RoutedEventArgs e)
        {
            var os = new OpenSector(this);
            os.Show();
        }

        public void Refresh()
        {
            if (CurrentSystem == null) return;
            SysID.Content = $"{CurrentSystem.ID}:";
            SysName.Text = CurrentSystem.Name;
            GMNotes.Text = CurrentSystem.Notes;
            Routes.Items.Clear();
            foreach (var connectedSystem in CurrentSystem.ConnectedSystems)
            {
                Routes.Items.Add(connectedSystem);
            }
            Systems.Items.Clear();
            foreach (var connectedSystem in SlipDrive.VisitedSystems)
            {
                Systems.Items.Add(connectedSystem);
            }
            if(!string.IsNullOrWhiteSpace(SlipDrive.NavigationJumpMessage))
            MessageBox.Show(SlipDrive.NavigationJumpMessage,"Result of Jump",MessageBoxButton.OK,MessageBoxImage.Information);
            PlotCurrentSystem.Content = CurrentSystem;
            PlotDestinationSystem.Items.Clear();
            foreach (var system in SlipDrive.VisitedSystems)
            {
                PlotDestinationSystem.Items.Add(system);
            }
        }

        private StarSystem CurrentSystem => SlipDrive.CurrentSystem;

        private void SysName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentSystem != null) CurrentSystem.Name = SysName.Text;
        }

        private void GMNotes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentSystem != null) CurrentSystem.Notes = GMNotes.Text;
        }

        private void OverrideCurrentSystem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SlipDrive.OverrideCurrentSystem(int.Parse(SystemOverride.Text));
                Refresh();
            }
            catch
            {
                // ignored
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SlipDrive.SaveSlipMap();
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(SessionFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            var session = new LastSession
            {
                FileName = SlipDrive.FileName,
                PilotSkill = PilotSkillLevel
            };
            try
            {
                formatter.Serialize(stream, session);
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

        private void SaveSector_OnClick(object sender, RoutedEventArgs e)
        {
            
            SlipDrive.SaveSlipMap();
            
        }

        private void BlindJump_Click(object sender, RoutedEventArgs e)
        {
            if (!PilotSkill.Text.Equals("Unset"))
            {
                SlipDrive.BlindJump(computer: false, pilotSkillLevel: PilotSkillLevel);
                Refresh();
            }
            else
            {
                MessageBox.Show("Set pilot skill and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Question);
                PilotSkill.Focus();
            }
        }

        private void NavJump_Click(object sender, RoutedEventArgs e)
        {
            if (!PilotSkill.Text.Equals("Unset"))
            {
                SlipDrive.NavigationJump(false, ((StarSystem) Routes.SelectedItem), PilotSkillLevel);
                Refresh();
            }
            else
            {
                MessageBox.Show("Set pilot skill and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Question);
                PilotSkill.Focus();
            }
        }

        private void Systems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Systems.SelectedItem != null) SystemNotes.Text = ((StarSystem) Systems.SelectedItem).Notes;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new About().Show();
        }

        private void CreateSlipRoute_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var origin = SlipDrive.VisitedSystems.FirstOrDefault(system => system.ID == int.Parse(CreateOrigin.Text));
                var destination =
                    SlipDrive.VisitedSystems.FirstOrDefault(system => system.ID == int.Parse(CreateDestination.Text));
                if(origin==null||destination==null)throw new Exception("Both systems have to be visited at least once.");
                SlipDrive.CreateSlipRoute(origin,destination);
                Refresh();
                
            }
            catch (Exception exception)
            {
                SlipError.Text = exception.Message;
            }

        }

        private void CleanUp_Click(object sender, RoutedEventArgs e)
        {
            SlipDrive.Clean();
            Refresh();
        }

        private void PlotDestinationSystem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var starSystem = PlotDestinationSystem.SelectedValue as StarSystem;
            if (starSystem == null) return;
            var route = SlipDrive.FindRoute(starSystem.ID);
            Jumps.Content = route.Count-1;
            RouteList.Items.Clear();
            route.Reverse();
            foreach (var system in route)
            {
                RouteList.Items.Add(system);
            }
        }
    }

    [Serializable]
    public class LastSession
    {
        public string FileName { get; set; }
        public int PilotSkill { get; set; }
    }
}
