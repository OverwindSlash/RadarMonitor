using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            RenderOptions.ProcessRenderMode = RenderMode.Default;

            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey =
                "AAPKb6c1f4f581bb46cdaf2b8e284b7ce498_DYJOMxTSnmYxcaXbAAIBqMIadJAO9jdGS06kqh0f5rfEVN8syaMRjpwgUq2Eg_g";
        }
    }
}
