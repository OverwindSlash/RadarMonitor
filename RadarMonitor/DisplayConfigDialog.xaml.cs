using RadarMonitor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for DisplayConfigDialog.xaml
    /// </summary>
    public partial class DisplayConfigDialog : Window
    {
        public static readonly Color DefaultImageEchoColor = Colors.Lime;

        public DisplayConfigDialog(Color scanlineColor, bool isFadingEnabled, int fadingInterval,
            double echoThreshold, double echoRadius, double echoMaxDistance)
        {
            InitializeComponent();

            DataContext = new DisplayConfigViewModel(scanlineColor, isFadingEnabled, fadingInterval, echoThreshold, echoRadius, echoMaxDistance);
        }

        private void BtnPickColor_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RadioBoxFading_OnChecked(object sender, RoutedEventArgs e)
        {
            var viewModel = (DisplayConfigViewModel)DataContext;
            var radioButton = (RadioButton)sender;

            if (radioButton.IsChecked.HasValue)
            {
                viewModel.IsFadingEnabled = radioButton.IsChecked.Value;
            }
        }
    }
}
