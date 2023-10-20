using CAT240Parser;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Hydrography;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using RadarMonitor.Model;
using RadarMonitor.ViewModel;
using Silk.WPF.OpenGL.Scene;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using Window = System.Windows.Window;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double _dpiX;
        private double _dpiY;
        private double _dpcX;
        private double _dpcY;

        private WriteableBitmap _bitmap;
        private const int ImageSize = RadarMonitorViewModel.CartesianSize;
        private byte[] _echoData;
        private int _echoDataStride;

        private DispatcherTimer _timer = new DispatcherTimer();
        private const int RefreshIntervalMs = 16;   // 60 FPS

        private Color _scanlineColor;
        private bool _isFadingEnabled = true;
        private int _fadingInterval = 3;
        private byte _fadingStep;

        private bool _encDetailDisplayFlag = false;


        public MainWindow()
        {
            InitializeComponent();

            // ViewModel 与事件注册
            var viewModel = new RadarMonitorViewModel();
            viewModel.OnEncChanged += OnEncChanged;
            viewModel.OnMainRadarChanged += OnMainRadarChanged;
            viewModel.OnCat240SpecChanged += OnCat240SpecChanged;
            viewModel.OnCat240PackageReceived += OnCat240PackageReceived;
            DataContext = viewModel;

            InitializeMapView();

            // 初始化图片回波显示后台一维数组
            _scanlineColor = DisplayConfigDialog.DefaultImageEchoColor;
            InitializeEchoData();
            
            // 初始化刷新计时器
            _timer.Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs);
            _timer.Tick += RefreshImageEcho;
            _timer.Start();

            LoadUserConfiguration();
        }

        private void InitializeMapView()
        {
            BaseMapView.Map = new Map();
            BaseMapView.IsAttributionTextVisible = false;

            #region Enc configuration
            // 海图显示配置：不显示 海床，深度点，地理名称等
            EncEnvironmentSettings.Default.DisplaySettings.MarinerSettings.ColorScheme = EncColorScheme.Dusk;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.AllIsolatedDangers = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ArchipelagicSeaLanes = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BoundariesAndLimits = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BuoysBeaconsAidsToNavigation = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BuoysBeaconsStructures = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ChartScaleBoundaries = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.DepthContours = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.DryingLine = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Lights = true;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.MagneticVariation = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.OtherMiscellaneous = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ProhibitedAndRestrictedAreas = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Seabed = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ShipsRoutingSystemsAndFerryRoutes = true;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SpotSoundings = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.StandardMiscellaneous = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SubmarineCablesAndPipelines = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Tidal = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.BerthNumber = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.CurrentVelocity = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.GeographicNames = true;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.HeightOfIsletOrLandFeature = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.ImportantText = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.LightDescription = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.MagneticVariationAndSweptDepth = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NamesForPositionReporting = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NatureOfSeabed = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NoteOnChartData = _encDetailDisplayFlag;
            #endregion

            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.IsEncLoaded = false;
            viewModel.IsEncDisplayed = false;
        }

        private void InitializeEchoData()
        {
            _echoData = new byte[RadarMonitorViewModel.CartesianSize * RadarMonitorViewModel.CartesianSize * 4];
            _echoDataStride = RadarMonitorViewModel.CartesianSize * 4;

            for (int i = 0; i < _echoData.Length; i += 4)
            {
                _echoData[i + 0] = _scanlineColor.B;    // Blue
                _echoData[i + 1] = _scanlineColor.G;    // Green
                _echoData[i + 2] = _scanlineColor.R;    // Red
                _echoData[i + 3] = 0;                   // Alpha
            }

            _bitmap = new WriteableBitmap(ImageSize, ImageSize, 96, 96, PixelFormats.Bgra32, null);
            EchoImageOverlay.Source = _bitmap;

            _fadingStep = (byte)Math.Max(1, 255 / _fadingInterval / (1000 / RefreshIntervalMs));
        }

        private async Task LoadUserConfiguration()
        {
            var viewModel = (RadarMonitorViewModel)DataContext;

            try
            {
                var configuration = viewModel.Configuration;

                var encConfiguration = configuration.EncConfiguration;
                if (encConfiguration != null && encConfiguration.EncEnabled)
                {
                    switch (encConfiguration.EncType.ToLower())
                    {
                        case "catalog":
                            await LoadEncByCatalog(encConfiguration.EncUri);
                            break;
                        case "dir":
                            LoadEncByDir(encConfiguration.EncUri);
                            break;
                        case "cell":
                            LoadEncByCell(encConfiguration.EncUri);
                            break;
                        default:
                            await LoadEncByCatalog(encConfiguration.EncUri);
                            break;
                    }

                    BaseMapView.SetViewpointAsync(new Viewpoint(
                        new MapPoint(encConfiguration.EncLongitude, encConfiguration.EncLatitude, SpatialReferences.Wgs84),
                        encConfiguration.EncScale));
                }
            }
            catch (Exception e)
            {

            }
        }

        private void RefreshImageEcho(object? sender, EventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;
            if (!viewModel.IsEchoDisplayed)
            {
                return;
            }

            // 重绘图片雷达回波
            if (viewModel.IsEchoDisplayed)
            {
                var cartesianData = viewModel.CartesianData;
                for (int x = 0; x < cartesianData.GetLength(0) - 1; x++)
                {
                    for (int y = 0; y < cartesianData.GetLength(1) - 1; y++)
                    {
                        int index = y * _echoDataStride + x * 4;

                        _echoData[index + 3] = (byte)cartesianData[x, y]; // Update Alpha

                        if (_isFadingEnabled)
                        {
                            cartesianData[x, y] = Math.Max(cartesianData[x, y] - _fadingStep, 0);
                        }
                    }
                }
            }

            _bitmap.WritePixels(new Int32Rect(0, 0, ImageSize, ImageSize), _echoData, _echoDataStride, 0);
            EchoOverlay.InvalidateVisual();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow);

            _dpiX = 96;
            _dpiY = 96;

            double adjustRatio = 1.173;  // 为了让屏幕上实际显示的线段长度更准确而人为引入的调整参数

            _dpcX = _dpiX / 2.54 * adjustRatio;
            _dpcY = _dpiY / 2.54 * adjustRatio;
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.DisposeCat240Parser();
        }
        
        #region Load ENC
        private async void LoadEnc_OnClick(object sender, RoutedEventArgs e)
        {
            // 指定海图文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Enc Catalog file (*.031)|*.031|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            string catalogFile = openFileDialog.FileName;

            try
            {
                await LoadEncByCatalog(catalogFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadEncByCatalog(string catalogFile)
        {
            // 加载海图
            InitializeMapView();

            EncExchangeSet encExchangeSet = new EncExchangeSet(catalogFile);
            await encExchangeSet.LoadAsync();

            List<Envelope> dataSetExtents = new List<Envelope>();
            foreach (EncDataset encDataset in encExchangeSet.Datasets)
            {
                EncLayer encLayer = new EncLayer(new EncCell(encDataset));
                await encLayer.LoadAsync();
                dataSetExtents.Add(encLayer.FullExtent);

                BaseMapView.Map.OperationalLayers.Add(encLayer);
            }

            Envelope fullExtent = GeometryEngine.CombineExtents(dataSetExtents);
            await BaseMapView.SetViewpointAsync(new Viewpoint(fullExtent));

            // 设置 ViewModel
            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.RecordNewEncUri(catalogFile);
        }

        private async void LoadCellDir_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new();

            folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string encDir = folderDialog.SelectedPath;

                LoadEncByDir(encDir);
            }
        }

        private void LoadEncByDir(string encDir)
        {
            InitializeMapView();

            List<string> files = new List<string>();
            GetAllFiles(encDir, files);
            foreach (string file in files)
            {
                if (!file.EndsWith(".000"))
                {
                    continue;
                }

                try
                {
                    var cell = new EncCell(file);
                    var layer = new EncLayer(cell) { Name = new FileInfo(file).Name };

                    var idx = layer.Name[2];
                    int insertIndex = 0;

                    foreach (var l in BaseMapView.Map.OperationalLayers)
                    {
                        var name = l.Name[2];
                        if (name > idx)
                        {
                            break;
                        }

                        insertIndex++;
                    }

                    BaseMapView.Map.OperationalLayers.Insert(insertIndex, layer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // 设置 ViewModel
            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.RecordNewEncUri(encDir);
        }

        private void LoadCell_OnClick(object sender, RoutedEventArgs e)
        {
            // 指定海图文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Enc Cell file (*.000)|*.000|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            string cellFile = openFileDialog.FileName;

            LoadEncByCell(cellFile);
        }

        private void LoadEncByCell(string cellFile)
        {
            InitializeMapView();

            try
            {
                var cell = new EncCell(cellFile);
                var layer = new EncLayer(cell) { Name = new FileInfo(cellFile).Name };

                var idx = layer.Name[2];
                int insertIndex = 0;

                foreach (var l in BaseMapView.Map.OperationalLayers)
                {
                    var name = l.Name[2];
                    if (name > idx)
                    {
                        break;
                    }

                    insertIndex++;
                }

                BaseMapView.Map.OperationalLayers.Insert(insertIndex, layer);

                // 设置 ViewModel
                var viewModel = (RadarMonitorViewModel)DataContext;
                viewModel.RecordNewEncUri(cellFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnEncChanged(object sender, string encuri)
        {
            TbStatusInfo.Text = $"ENC {encuri} loaded successfully.";
        }
        #endregion

        #region Connect Radar
        private void ConnectRadar_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;
            if (!viewModel.IsEncLoaded)
            {
                MessageBox.Show("Please load Enc first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 指定雷达参数对话框
            var radarDialog = new RadarDialog(viewModel.Configuration.RadarConfigurations);
            bool? dialogResult = radarDialog.ShowDialog();
            if (dialogResult == true)
            {
                var settings = (RadarSettingsViewModel)radarDialog.DataContext;

                // 获取雷达经纬度和网络信息
                viewModel.MainRadarSettings = settings.ToRadarSettings();
                viewModel.IsRadarConnected = true;
                viewModel.IsRingsDisplayed = true;

                // 默认显示图片雷达回波
                bool showImageEchoFirst = true;
                viewModel.IsEchoDisplayed = showImageEchoFirst;
                viewModel.IsOpenGlEchoDisplayed = !showImageEchoFirst;
                OpenGlEchoOverlay.IsDisplay = viewModel.IsOpenGlEchoDisplayed;
                OpenGlEchoOverlay.Visibility = viewModel.IsOpenGlEchoDisplayed ? Visibility.Visible : Visibility.Hidden;

                // 抓取 CAT240 网络包
                InitializeEchoData();
                viewModel.CaptureCat240NetworkPackage();

                DrawRings(viewModel.RadarLongitude, viewModel.RadarLatitude);
                TransformRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale, 0);
                TransformOpenGlRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale, 0);
            }
        }

        private void OnMainRadarChanged(object sender, RadarSettings radarSettings)
        {
            TbStatusInfo.Text = $"Listening radar on {radarSettings.RadarIpAddress}:{radarSettings.RadarPort}.";
            InitializeEchoData();
        }

        private void OnCat240SpecChanged(object sender, Cat240Spec cat240spec)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var viewModel = (RadarMonitorViewModel)DataContext;
                    TransformRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale,
                        cat240spec.MaxDistance);
                    TransformOpenGlRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale,
                        cat240spec.MaxDistance);

                    // TODO: 疑问点
                    OpenGlEchoOverlay.RadarOrientation = viewModel.RadarOrientation;
                    OpenGlEchoOverlay.RadarMaxDistance = viewModel.MaxDistance;
                    OpenGlEchoOverlay.RealCells = viewModel.CellCount;
                });
            }
            catch (Exception e)
            {
                // TODO: 有没有更好的优雅结束的方法
            }
        }

        private void OnCat240PackageReceived(object sender, Cat240DataBlock data)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var viewModel = (RadarMonitorViewModel)DataContext;

                    // OpenGL 回波图像绘制
                    if (viewModel.IsOpenGlEchoDisplayed)
                    {
                        // TODO: 疑问点
                        OpenGlEchoOverlay.RadarOrientation = viewModel.RadarOrientation;
                        OpenGlEchoOverlay.RadarMaxDistance = viewModel.MaxDistance;
                        OpenGlEchoOverlay.RealCells = viewModel.CellCount;
                        ExampleScene.OnReceivedCat240DataBlock(sender, data);
                    }
                });
            }
            catch (Exception e)
            {
                // TODO: 有没有更好的优雅结束的方法
            }
        }
        #endregion


        private void ConfigDisplay_OnClick(object sender, RoutedEventArgs e)
        {
            var configDialog = new DisplayConfigDialog(_scanlineColor, _isFadingEnabled, _fadingInterval);
            bool? dialogResult = configDialog.ShowDialog();

            if (dialogResult == true)
            {
                var config = (DisplayConfigViewModel)configDialog.DataContext;

                _scanlineColor = config.ScanlineColor;
                // Change Color
                for (int i = 0; i < _echoData.Length; i += 4)
                {
                    byte alpha = _echoData[i + 3];
                    if (alpha == 0)
                    {
                        continue;
                    }

                    _echoData[i + 0] = _scanlineColor.B;
                    _echoData[i + 1] = _scanlineColor.G;
                    _echoData[i + 2] = _scanlineColor.R;
                }

                _isFadingEnabled = config.IsFadingEnabled;
                _fadingInterval = config.FadingInterval;
            }
        }

        private void BaseMapView_OnViewpointChanged(object? sender, EventArgs e)
        {
            MapView mapView = (MapView)sender;
            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.CurrentEncScale = mapView.MapScale;

            if (viewModel.IsEncLoaded)
            {
                DrawScaleLine();
            }

            if (viewModel.IsEchoDisplayed || viewModel.IsOpenGlEchoDisplayed)
            {
                DrawRings(viewModel.RadarLongitude, viewModel.RadarLatitude);
                TransformRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale, viewModel.MaxDistance);
                TransformOpenGlRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale, viewModel.MaxDistance);
            }
        }

        private void BaseMapView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            MapView mapView = (MapView)sender;
            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.CurrentEncScale = mapView.MapScale;

            if (viewModel.IsEncLoaded)
            {
                DrawScaleLine();
            }

            if (viewModel.IsEchoDisplayed || viewModel.IsOpenGlEchoDisplayed)
            {
                DrawRings(viewModel.RadarLongitude, viewModel.RadarLatitude);
                TransformRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale, viewModel.MaxDistance);
                TransformOpenGlRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale,
                    viewModel.MaxDistance);
            }
        }

        private void BaseMapView_OnMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(BaseMapView);

            Point screenPoint = new Point(mousePosition.X, mousePosition.Y);
            var location = BaseMapView.ScreenToLocation(screenPoint);
            if (location == null)
            {
                return;
            }

            // 鼠标当前经纬度
            CursorLongitude.Content = "Lon: " + location.X.ToString("F6");
            CursorLatitude.Content = "Lat:    " + location.Y.ToString("F6");

            // 鼠标相对于雷达经纬度
            var viewModel = (RadarMonitorViewModel)DataContext;
            if ((viewModel.RadarLongitude != 0.0) && (viewModel.RadarLatitude != 0.0))
            {
                var distance = CalculateDistance(viewModel.RadarLongitude, viewModel.RadarLatitude, location.X, location.Y);
                CursorDistance.Content = "D2R:   " + distance.ToString("F2") + " KM";

                var azimuth = CalculateAzimuth(viewModel.RadarLongitude, viewModel.RadarLatitude,location.X, location.Y);
                CursorAzimuth.Content = "A2R:   " + azimuth.ToString("F2") + "°";
            }
        }

        private void DisplayOpenGlEcho_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayOpenGlEchoCheckBox = (CheckBox)sender;
            var viewModel = (RadarMonitorViewModel)DataContext;

            if (displayOpenGlEchoCheckBox.IsChecked.Value)
            {
                OpenGlEchoOverlay.Visibility = Visibility.Visible;
                OpenGlEchoOverlay.IsDisplay = true;
            }
            else
            {
                OpenGlEchoOverlay.Visibility = Visibility.Hidden;
                OpenGlEchoOverlay.IsDisplay = false;
            }
        }

        private void DisplayEcho_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                EchoImageOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                EchoImageOverlay.Visibility = Visibility.Hidden;
            }
        }

        private void DisplayRings_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayRingsCheckBox = (CheckBox)sender;

            if (displayRingsCheckBox.IsChecked.Value)
            {
                RingsOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                RingsOverlay.Visibility = Visibility.Hidden;
            }
        }

        private void DisplayEnc_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEncCheckBox = (CheckBox)sender;

            if (displayEncCheckBox.IsChecked.Value)
            {
                BaseMapView.Visibility = Visibility.Visible;
                ScaleOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                BaseMapView.Visibility = Visibility.Hidden;
                ScaleOverlay.Visibility = Visibility.Hidden;
            }
        }

        #region Map Operations
        private void GotoViewPoint(PresetLocation location)
        {
            MapPoint mapPoint = new MapPoint(location.Longitude, location.Latitude, SpatialReferences.Wgs84);
            Viewpoint viewpoint = new Viewpoint(mapPoint, location.Scale);
            BaseMapView.SetViewpoint(viewpoint);
        }

        private void MoveByDirection(string direction, double step = 1.0)
        {
            Viewpoint currentViewpoint = BaseMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            if (currentViewpoint == null)
            {
                return;
            }

            var scale = currentViewpoint.TargetScale;
            double moveUnit = step * scale / 10000000.0;

            MapPoint centerPoint = currentViewpoint.TargetGeometry as MapPoint;
            MapPoint newCenterPoint = null;

            switch (direction)
            {
                case "Upper-Left":
                    newCenterPoint = new MapPoint(centerPoint.X - moveUnit, centerPoint.Y + moveUnit, centerPoint.SpatialReference);
                    break;
                case "Up":
                    newCenterPoint = new MapPoint(centerPoint.X, centerPoint.Y + moveUnit, centerPoint.SpatialReference);
                    break;
                case "Upper-Right":
                    newCenterPoint = new MapPoint(centerPoint.X + moveUnit, centerPoint.Y + moveUnit, centerPoint.SpatialReference);
                    break;
                case "Left":
                    newCenterPoint = new MapPoint(centerPoint.X - moveUnit, centerPoint.Y, centerPoint.SpatialReference);
                    break;
                case "Center":
                    var viewModel = (RadarMonitorViewModel)DataContext;
                    if ((viewModel.RadarLongitude != 0.0) && (viewModel.RadarLatitude != 0.0))
                    {
                        newCenterPoint = new MapPoint(viewModel.RadarLongitude, viewModel.RadarLatitude, SpatialReferences.Wgs84);
                    }
                    else
                    {
                        newCenterPoint = new MapPoint(centerPoint.X, centerPoint.Y, centerPoint.SpatialReference);
                    }
                    break;
                case "Right":
                    newCenterPoint = new MapPoint(centerPoint.X + moveUnit, centerPoint.Y, centerPoint.SpatialReference);
                    break;
                case "Lower-Left":
                    newCenterPoint = new MapPoint(centerPoint.X - moveUnit, centerPoint.Y - moveUnit, centerPoint.SpatialReference);
                    break;
                case "Down":
                    newCenterPoint = new MapPoint(centerPoint.X, centerPoint.Y - moveUnit, centerPoint.SpatialReference);
                    break;
                case "Lower-Right":
                    newCenterPoint = new MapPoint(centerPoint.X + moveUnit, centerPoint.Y - moveUnit, centerPoint.SpatialReference);
                    break;
                default:
                    newCenterPoint = new MapPoint(centerPoint.X, centerPoint.Y, centerPoint.SpatialReference);
                    break;
            }

            Viewpoint newViewpoint = new Viewpoint(newCenterPoint, scale);
            BaseMapView.SetViewpoint(newViewpoint);
        }

        private void ZoomView(bool isZoomIn, double step = 1.1)
        {
            Viewpoint currentViewpoint = BaseMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);

            double scaleDelta = isZoomIn ? 1.0 / step : step;

            MapPoint mapPoint = currentViewpoint.TargetGeometry as MapPoint;
            double newScale = currentViewpoint.TargetScale * scaleDelta;

            Viewpoint newViewpoint = new Viewpoint(mapPoint, newScale);
            BaseMapView.SetViewpoint(newViewpoint);
        }

        private double CalculateDistance(double lon1, double lat1, double lon2, double lat2)
        {
            lat1 = lat1 * (Math.PI / 180);
            lon1 = lon1 * (Math.PI / 180);
            lat2 = lat2 * (Math.PI / 180);
            lon2 = lon2 * (Math.PI / 180);

            double earthRadius = 6371.0; // 地球半径（以公里为单位）

            double dlon = lon2 - lon1;
            double dlat = lat2 - lat1;

            double a = Math.Sin(dlat / 2) * Math.Sin(dlat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dlon / 2) * Math.Sin(dlon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = earthRadius * c;

            return distance;
        }

        static double CalculateAzimuth(double lon1, double lat1, double lon2, double lat2)
        {
            lat1 = lat1 * (Math.PI / 180);
            lon1 = lon1 * (Math.PI / 180);
            lat2 = lat2 * (Math.PI / 180);
            lon2 = lon2 * (Math.PI / 180);

            // 计算差值
            double dlon = lon2 - lon1;

            // 使用反正切函数计算方位角
            double y = Math.Sin(dlon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dlon);
            double azimuth = Math.Atan2(y, x);

            // 将弧度转换为度数
            azimuth = azimuth * (180.0 / Math.PI);

            // 将负角度转换为正角度
            if (azimuth < 0)
            {
                azimuth += 360.0;
            }

            return azimuth;
        }

        private void DrawScaleLine()
        {
            double mapScale = BaseMapView.MapScale;
            double kmWith1Cm = (mapScale / 100000.0);

            Brush scaleBrush = new SolidColorBrush(Colors.LimeGreen);
            int scaleLength = 3;
            int markHeight = 4;
            int labelXOffset = 5;
            int labelYOffset = 10;
            int labelFontSize = 10;

            Canvas.SetLeft(ScaleOverlay, 50);
            Canvas.SetTop(ScaleOverlay, 50);
            double canvasWidth = ScaleOverlay.ActualWidth;
            double canvasHeight = ScaleOverlay.ActualHeight;
            double lineHeight = canvasHeight / 2;

            ScaleOverlay.Children.Clear();

            // 0点标签
            TextBlock zeroLabel = new TextBlock
            {
                Text = "0",
                Foreground = scaleBrush,
                FontSize = labelFontSize,
                Margin = new Thickness(0, canvasHeight / 2 + labelYOffset, 0, 0)
            };
            ScaleOverlay.Children.Add(zeroLabel);

            // 比例尺
            TextBlock scaleLabel = new TextBlock
            {
                Text = $"Scale 1:{(int)mapScale}",
                Foreground = scaleBrush,
                FontSize = labelFontSize,
                Margin = new Thickness(0, 0, 0, 0)
            };
            ScaleOverlay.Children.Add(scaleLabel);

            // 0点刻度线
            Line zeroMark = new Line
            {
                X1 = 0,
                Y1 = lineHeight,
                X2 = 0,
                Y2 = lineHeight - markHeight,
                Stroke = scaleBrush,
                StrokeThickness = 1
            };
            ScaleOverlay.Children.Add(zeroMark);

            for (int i = 0; i < scaleLength; i++)
            {
                int begin = i;
                int end = i + 1;

                // 标尺线
                Line scaleLine = new Line
                {
                    X1 = begin * _dpcX,
                    Y1 = lineHeight,
                    X2 = end * _dpcX,
                    Y2 = lineHeight,
                    Stroke = scaleBrush,
                    StrokeThickness = 2
                };
                ScaleOverlay.Children.Add(scaleLine);

                // 标尺尾部刻度线
                Line mark = new Line
                {
                    X1 = end * _dpcX,
                    Y1 = lineHeight,
                    X2 = end * _dpcX,
                    Y2 = lineHeight - markHeight,
                    Stroke = scaleBrush,
                    StrokeThickness = 1
                };
                ScaleOverlay.Children.Add(mark);

                // 标尺尾部标签
                TextBlock TailLabel = new TextBlock
                {
                    Text = (end * kmWith1Cm).ToString("F1"),
                    Foreground = scaleBrush,
                    FontSize = labelFontSize,
                    Margin = new Thickness(end * _dpcX - labelXOffset, canvasHeight / 2 + labelYOffset, 0, 0)
                };
                ScaleOverlay.Children.Add(TailLabel);
            }

            // KM尾标
            TextBlock kmLabel = new TextBlock
            {
                Text = "(KM)",
                Foreground = scaleBrush,
                FontSize = labelFontSize,
                Margin = new Thickness(scaleLength * _dpcX + labelXOffset, canvasHeight / 2 - labelYOffset, 0, 0)
            };
            ScaleOverlay.Children.Add(kmLabel);
        }

        private void DrawRings(double longitude, double latitude)
        {
            RingsOverlay.Children.Clear();

            Brush ringBrush = new SolidColorBrush(Colors.Chartreuse);   // 距离环的颜色
            DoubleCollection dashArray = new DoubleCollection(new double[] { 2, 4 });
            int ringFontSize = 12;

            // 定义雷达回波的圆心坐标
            var point = BaseMapView.LocationToScreen(new MapPoint(longitude, latitude, SpatialReferences.Wgs84));
            double radarX = point.X;
            double radarY = point.Y;

            double maxDistance = Math.Max(RingsOverlay.ActualWidth, RingsOverlay.ActualHeight) / 2;
            
            // 绘制雷达点
            double radarPointRadius = 1.0;
            Ellipse center = new Ellipse
            {
                Width = 2 * radarPointRadius,
                Height = 2 * radarPointRadius,
                Stroke = ringBrush,
                StrokeThickness = 0,
                Fill = ringBrush
            };
            Canvas.SetLeft(center, radarX - radarPointRadius);
            Canvas.SetTop(center, radarY - radarPointRadius);
            RingsOverlay.Children.Add(center);

            // 绘制距离环 (以下所有单位缺省都为像素)
            int ringOffset = 0;
            int ringStep = 2;
            double ringInterval = 2 * _dpcX;
            for (double radius = 0.0; radius < maxDistance; radius += ringInterval)
            {
                Ellipse circle = new Ellipse
                {
                    Width = 2 * radius,
                    Height = 2 * radius,
                    Stroke = ringBrush,
                    StrokeDashArray = dashArray,
                    StrokeThickness = 1,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(circle, radarX - radius);
                Canvas.SetTop(circle, radarY - radius);
                RingsOverlay.Children.Add(circle);

                // 添加距离文字
                double radiusInKm = ringOffset * (BaseMapView.MapScale / 100000.0);
                ringOffset += ringStep;

                if (radiusInKm == 0)
                {
                    continue;   // 不显示 0KM
                }

                TextBlock distanceText = new TextBlock();
                distanceText.Text = radiusInKm.ToString("F1") + "KM";
                distanceText.Foreground = ringBrush;
                distanceText.FontSize = ringFontSize;

                Canvas.SetLeft(distanceText, radarX + radius + 2);
                Canvas.SetTop(distanceText, radarY);
                RingsOverlay.Children.Add(distanceText);
            }
        }

        private void TransformRadarEcho(double longitude, double latitude, double scale, double maxDistance)
        {
            if ((longitude == 0) || (latitude == 0) || (scale == 0))
            {
                return;
            }

            double kmWith1Cm = (scale / 100000.0);
            double kmWith1px = kmWith1Cm / _dpcX;

            EchoOverlay.Children.Clear();
            EchoOverlay.Children.Add(EchoImageOverlay);

            var size = 2* maxDistance / kmWith1px;

            EchoImageOverlay.Width = size;
            EchoImageOverlay.Height = size;

            var point = BaseMapView.LocationToScreen(new MapPoint(longitude, latitude, SpatialReferences.Wgs84));

            Canvas.SetLeft(EchoImageOverlay, point.X - size / 2.0);
            Canvas.SetTop(EchoImageOverlay, point.Y - size / 2.0);
        }

        private void TransformOpenGlRadarEcho(double longitude, double latitude, double scale, double maxDistance)
        {
            if ((longitude == 0) || (latitude == 0) || (scale == 0))
            {
                return;
            }

            double kmWith1Cm = (scale / 100000.0);
            double kmWith1Px = kmWith1Cm / _dpcX;
            double kmWith1Lon = 111.32;

            int uiWidth = (int)OpenGlEchoOverlay.ActualWidth;
            int uiHeight = (int)OpenGlEchoOverlay.ActualHeight;
            float mapWidth = (float)(uiWidth * kmWith1Px);
            float mapHeight = (float)(uiHeight * kmWith1Px);

            Viewpoint center = BaseMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            MapPoint centerPoint = center.TargetGeometry as MapPoint;

            double xOffset = longitude - centerPoint.X;
            double yOffset = latitude - centerPoint.Y;

            float mapWidthOffCenter = (float)(xOffset * kmWith1Lon);
            float mapHeightOffCenter = (float)(yOffset * kmWith1Lon);

            OpenGlEchoOverlay.UIWidth = uiWidth;
            OpenGlEchoOverlay.UIHeight = uiHeight;
            OpenGlEchoOverlay.MapWidth = mapWidth;
            OpenGlEchoOverlay.MapHeight = mapHeight;
            OpenGlEchoOverlay.MapWidthOffCenter = mapWidthOffCenter;
            OpenGlEchoOverlay.MapHeightOffCenter = mapHeightOffCenter;

            var viewModel = (RadarMonitorViewModel)DataContext;
            OpenGlEchoOverlay.RadarOrientation = viewModel.RadarOrientation;
        }
        #endregion

        #region Preset Locations
        private void BtnPresetLocation1_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;

            GotoViewPoint(viewModel.GetPresetLocation(0));
        }

        private void BtnPresetLocation2_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;

            GotoViewPoint(viewModel.GetPresetLocation(1));
        }

        private void BtnPresetLocation3_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;

            GotoViewPoint(viewModel.GetPresetLocation(2));
        }

        private void BtnPresetLocation4_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;

            GotoViewPoint(viewModel.GetPresetLocation(3));
        }

        private void BtnPresetLocation5_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;

            GotoViewPoint(viewModel.GetPresetLocation(4));
        }
        #endregion

        #region Navigation
        private void BtnUpperLeft_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Upper-Left");
        }

        private void BtnUp_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Up");
        }

        private void BtnUpperRight_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Upper-Right");
        }

        private void BtnLeft_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Left");
        }

        private void BtnCenter_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Center");
        }

        private void BtnRight_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Right");
        }

        private void BtnLowerLeft_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Lower-Left");
        }

        private void BtnDown_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Down");
        }

        private void BtnLowerRight_OnClick(object sender, RoutedEventArgs e)
        {
            MoveByDirection("Lower-Right");
        }

        private void BtnZoomIn_OnClick(object sender, RoutedEventArgs e)
        {
            ZoomView(true);
        }

        private void BtnZoomOut_OnClick(object sender, RoutedEventArgs e)
        {
            ZoomView(false);
        }
        #endregion

        static void GetAllFiles(string path, List<string> fileList)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] allFile = dir.GetFiles();
            foreach (FileInfo fi in allFile)
            {
                fileList.Add(fi.FullName);
            }
            DirectoryInfo[] allDir = dir.GetDirectories();
            foreach (DirectoryInfo d in allDir)
            {
                GetAllFiles(d.FullName, fileList);
            }
        }
    }
}
