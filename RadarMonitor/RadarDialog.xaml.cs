using RadarMonitor.Model;
using RadarMonitor.ViewModel;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for RadarDialog.xaml
    /// </summary>
    public partial class RadarDialog : Window
    {
        private List<RadarSettings> _settings;

        public RadarDialog()
        {
            InitializeComponent();

            LoadPresets();

            DataContext = new RadarSettingsViewModel();
        }

        private void LoadPresets()
        {
            string contents = File.ReadAllText("Presets/preset-radars.yaml");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            //yml contains a string containing your YAML
            _settings = deserializer.Deserialize<List<RadarSettings>>(contents);
        }

        private void BtnLoadPreset1_Click(object sender, RoutedEventArgs e)
        {
            DataContext = new RadarSettingsViewModel(_settings[0]);
        }

        private void BtnLoadPreset2_Click(object sender, RoutedEventArgs e)
        {
            DataContext = new RadarSettingsViewModel(_settings[1]);
        }

        private void BtnLoadPreset3_Click(object sender, RoutedEventArgs e)
        {
            DataContext = new RadarSettingsViewModel(_settings[2]);
        }

        private void BtnLoadPreset4_Click(object sender, RoutedEventArgs e)
        {
            DataContext = new RadarSettingsViewModel(_settings[3]);
        }

        private void BtnLoadPreset5_Click(object sender, RoutedEventArgs e)
        {
            DataContext = new RadarSettingsViewModel(_settings[4]);
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
