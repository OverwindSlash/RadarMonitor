using RadarMonitor.Model;
using RadarMonitor.ViewModel;
using System.Collections.Generic;
using System.Windows;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for RadarDialog.xaml
    /// </summary>
    public partial class RadarDialog : Window
    {
        private List<RadarSetting> _settings;

        public RadarDialog(List<RadarSetting> radarSettings)
        {
            InitializeComponent();

            _settings = radarSettings;

            DataContext = new RadarSettingsViewModel(_settings);
        }

        private void BtnLoadPreset1_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarSettingsViewModel)DataContext;
            viewModel.CurrentEditRadarSetting = viewModel.RadarSettings[0];
        }

        private void BtnLoadPreset2_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarSettingsViewModel)DataContext;
            viewModel.CurrentEditRadarSetting = viewModel.RadarSettings[1];
        }

        private void BtnLoadPreset3_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarSettingsViewModel)DataContext;
            viewModel.CurrentEditRadarSetting = viewModel.RadarSettings[2];
        }

        private void BtnLoadPreset4_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarSettingsViewModel)DataContext;
            viewModel.CurrentEditRadarSetting = viewModel.RadarSettings[3];
        }

        private void BtnLoadPreset5_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarSettingsViewModel)DataContext;
            viewModel.CurrentEditRadarSetting = viewModel.RadarSettings[4];
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarSettingsViewModel)DataContext;

            if (viewModel.IsValidated())
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
