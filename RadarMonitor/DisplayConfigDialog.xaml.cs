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
using RadarMonitor.ViewModel;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for DisplayConfigDialog.xaml
    /// </summary>
    public partial class DisplayConfigDialog : Window
    {
        public static readonly Color DefaultImageEchoColor = Colors.Crimson;

        public DisplayConfigDialog(Color scanlineColor, bool isFadingEnabled, int fadingInterval)
        {
            InitializeComponent();

            DataContext = new DisplayConfigViewModel(scanlineColor, isFadingEnabled, fadingInterval);
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
