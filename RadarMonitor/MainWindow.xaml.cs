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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Esri.ArcGISRuntime;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using Window = System.Windows.Window;
using System.Linq;
using System.Threading;

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

        private const int RadarCount = 5;

        private readonly List<Image> _echoImageOverlays = new();

        private const int ImageSize = RadarMonitorViewModel.CartesianSize;
        private const int RadarEchoDataStride = RadarMonitorViewModel.CartesianSize * 4;

        private List<WriteableBitmap> _radarBitmaps = new();
        private List<byte[]> _radarEchoDatas = new();

        private readonly DispatcherTimer _timer = new();
        private const int RefreshIntervalMs = 33;   // 30 FPS

        private Color _echoColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isFadingEnabled = true;
        private int _fadingInterval = 3;
        private byte _fadingStep;

        private readonly bool _encDetailDisplayFlag = false;
        private Int32Rect _redrawRect;


        public MainWindow()
        {
            InitializeComponent();

            // 初始化显示单位
            InitializeDisplayMetric();

            // 初始化ViewModel及注册事件
            InitializeViewModel();

            // 初始化 ArcGIS 地图控件
            InitializeMapView();
            InitializeMapOverlays();

            // 初始化图片回波显示后台一维数组
            InitializeEchoData();

            // 初始化刷新计时器
            InitializeRedrawTimer();

            // 如果有用户配置文件，则加载并预载入指定海图和雷达
            LoadUserConfiguration();
        }

        private void InitializeDisplayMetric()
        {
            _dpiX = 96;
            _dpiY = 96;

            double adjustRatio = 1.173; // 为了让屏幕上实际显示的线段长度更准确而人为引入的调整参数

            _dpcX = _dpiX / 2.54 * adjustRatio;
            _dpcY = _dpiY / 2.54 * adjustRatio;
        }

        private void InitializeViewModel()
        {
            // ViewModel 与事件注册
            var viewModel = new RadarMonitorViewModel();
            viewModel.OnEncChanged += OnEncChanged;
            viewModel.OnRadarChanged += OnRadarChanged;
            viewModel.OnRadarConnectionStatusChanged += OnRadarConnectionStatusChanged;
            viewModel.OnCat240SpecChanged += OnCat240SpecChanged;
            viewModel.OnCat240PackageReceived += OnCat240PackageReceived;
            DataContext = viewModel;
        }

        private void InitializeMapView()
        {
            BaseMapView.Map = new Map();
            BaseMapView.IsAttributionTextVisible = false;

            #region Enc configuration
            // 海图显示配置：不显示 海床，深度点，地理名称等
            EncEnvironmentSettings.Default.DisplaySettings.MarinerSettings.ColorScheme = EncColorScheme.Night;

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
        }

        private void InitializeMapOverlays()
        {
            _echoImageOverlays.Add(Radar1EchoImage);
            _echoImageOverlays.Add(Radar2EchoImage);
            _echoImageOverlays.Add(Radar3EchoImage);
            _echoImageOverlays.Add(Radar4EchoImage);
            _echoImageOverlays.Add(Radar5EchoImage);
        }

        // TODO: 多雷达支持
        private void InitializeEchoData()
        {
            _radarBitmaps.Clear();
            _radarEchoDatas.Clear();

            _redrawRect = new Int32Rect(0, 0, ImageSize, ImageSize);

            for (int radarId = 0; radarId < RadarCount; radarId++)
            {
                // 准备雷达回波数据二维数组
                var radarEchoData = new byte[RadarMonitorViewModel.CartesianSize * RadarMonitorViewModel.CartesianSize * 4];
                for (int i = 0; i < radarEchoData.Length; i += 4)
                {
                    radarEchoData[i + 0] = _echoColor.B;    // Blue
                    radarEchoData[i + 1] = _echoColor.G;    // Green
                    radarEchoData[i + 2] = _echoColor.R;    // Red
                    radarEchoData[i + 3] = 0;               // Alpha
                }
                _radarEchoDatas.Add(radarEchoData);

                // 准备雷达回波数据图像
                var radarBitmap = new WriteableBitmap(ImageSize, ImageSize, 96, 96, PixelFormats.Bgra32, null);
                _radarBitmaps.Add(radarBitmap);
                _echoImageOverlays[radarId].Source = radarBitmap;
            }

            _fadingStep = (byte)Math.Max(1, 255 / _fadingInterval / (1000 / RefreshIntervalMs));
        }

        private void InitializeRedrawTimer()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs);
            _timer.Tick += RefreshImageEcho;
            _timer.Start();
        }

        private async Task LoadUserConfiguration()
        {
            try
            {
                var configuration = GetViewModel().Configuration;

                var encSetting = configuration.EncSetting;
                if (encSetting != null && encSetting.EncEnabled)
                {
                    switch (encSetting.EncType.ToLower())
                    {
                        case "catalog":
                            await LoadEncByCatalog(encSetting.EncUri);
                            break;
                        case "dir":
                            LoadEncByDir(encSetting.EncUri);
                            break;
                        case "cell":
                            LoadEncByCell(encSetting.EncUri);
                            break;
                        default:
                            await LoadEncByCatalog(encSetting.EncUri);
                            break;
                    }

                    await BaseMapView.SetViewpointAsync(new Viewpoint(
                        new MapPoint(encSetting.EncLongitude, encSetting.EncLatitude, SpatialReferences.Wgs84),
                        encSetting.EncScale));
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        private void RefreshImageEcho(object? sender, EventArgs e)
        {
            var viewModel = GetViewModel();

            Task.Run(() =>
            {
                for (int radarId = 0; radarId < RadarCount; radarId++)
                {
                    var radarSetting = viewModel.RadarSettings[radarId];
                    if (radarSetting.IsConnected && radarSetting.IsEchoDisplayed)
                    {
                        // 重绘图片雷达回波
                        var radarEchoData = _radarEchoDatas[radarId];
                        var cartesianData = viewModel.RadarCartesianDatas[radarId];
                        for (int x = 0; x < cartesianData.GetLength(0) - 1; x++)
                        {
                            for (int y = 0; y < cartesianData.GetLength(1) - 1; y++)
                            {
                                int index = y * RadarEchoDataStride + x * 4;

                                radarEchoData[index + 3] = (byte)cartesianData[x, y]; // Update Alpha

                                if (_isFadingEnabled)
                                {
                                    cartesianData[x, y] = Math.Max(cartesianData[x, y] - _fadingStep, 0);
                                }
                            }
                        }

                        Dispatcher.Invoke(() =>
                        {
                            _radarBitmaps[radarId].WritePixels(_redrawRect, radarEchoData, RadarEchoDataStride, 0);
                            //_echoImageOverlays[radarId].InvalidateVisual();
                        });
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    RadarEchoOverlay.InvalidateVisual();
                });
            }).ConfigureAwait(true); // 使用ConfigureAwait(true)确保在UI线程上继续执行

            // Dispatcher.Invoke(() =>
            // {
            //     for (int radarId = 0; radarId < RadarCount; radarId++)
            //     {
            //         var radarSetting = viewModel.RadarSettings[radarId];
            //         if (radarSetting.IsConnected && radarSetting.IsEchoDisplayed)
            //         {
            //             // 重绘图片雷达回波
            //             var radarEchoData = _radarEchoDatas[radarId];
            //             var cartesianData = viewModel.RadarCartesianDatas[radarId];
            //             for (int x = 0; x < cartesianData.GetLength(0) - 1; x++)
            //             {
            //                 for (int y = 0; y < cartesianData.GetLength(1) - 1; y++)
            //                 {
            //                     int index = y * RadarEchoDataStride + x * 4;
            //
            //                     radarEchoData[index + 3] = (byte)cartesianData[x, y]; // Update Alpha
            //
            //                     if (_isFadingEnabled)
            //                     {
            //                         cartesianData[x, y] = Math.Max(cartesianData[x, y] - _fadingStep, 0);
            //                     }
            //                 }
            //             }
            //
            //             _radarBitmaps[radarId].WritePixels(redrawRect, radarEchoData, RadarEchoDataStride, 0);
            //             //_echoImageOverlays[radarId].InvalidateVisual();
            //         }
            //     }
            // }, DispatcherPriority.Render);

            //RadarEchoOverlay.InvalidateVisual();
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

            try
            {
                await LoadEncByCatalog(openFileDialog.FileName);
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

            // 记录海图类型及Uri
            GetViewModel().RecordEncTypeAndUri("Catalog", catalogFile);
        }

        private async void LoadCellDir_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new();

            folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            try
            {
                await LoadEncByDir(folderDialog.SelectedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadEncByDir(string encDir)
        {
            InitializeMapView();

            List<string> files = new List<string>();
            GetAllFiles(encDir, files);

            List<Envelope> dataSetExtents = new List<Envelope>();
            foreach (string file in files)
            {
                if (!file.EndsWith(".000"))
                {
                    continue;
                }

                EncLayer encLayer = new EncLayer(new EncCell(file));
                await encLayer.LoadAsync();
                dataSetExtents.Add(encLayer.FullExtent);

                BaseMapView.Map.OperationalLayers.Add(encLayer);
            }

            Envelope fullExtent = GeometryEngine.CombineExtents(dataSetExtents);
            await BaseMapView.SetViewpointAsync(new Viewpoint(fullExtent));

            // 记录海图类型及Uri
            GetViewModel().RecordEncTypeAndUri("Dir", encDir);
        }

        private async void LoadCell_OnClick(object sender, RoutedEventArgs e)
        {
            // 指定海图文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Enc Cell file (*.000)|*.000|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                await LoadEncByCell(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadEncByCell(string cellFile)
        {
            InitializeMapView();

            List<Envelope> dataSetExtents = new List<Envelope>();

            EncLayer encLayer = new EncLayer(new EncCell(cellFile));
            await encLayer.LoadAsync();
            dataSetExtents.Add(encLayer.FullExtent);

            BaseMapView.Map.OperationalLayers.Add(encLayer);

            Envelope fullExtent = GeometryEngine.CombineExtents(dataSetExtents);
            await BaseMapView.SetViewpointAsync(new Viewpoint(fullExtent));

            // 记录海图类型及Uri
            GetViewModel().RecordEncTypeAndUri("Cell", cellFile);
        }

        private void OnEncChanged(object sender, string encType, string encuri)
        {
            TbStatusInfo.Text = $"{encType} ENC at '{encuri}' loaded successfully.";
        }
        #endregion

        #region Radar Handler
        private void ConnectRadar_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = GetViewModel();
            if (!viewModel.IsEncLoaded)
            {
                MessageBox.Show("Please load Enc first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 指定雷达参数对话框
            var radarDialog = new RadarDialog(viewModel.Configuration.RadarSettings);
            bool? dialogResult = radarDialog.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }

            // 获取雷达静态信息
            var dialogViewModel = (RadarSettingsViewModel)radarDialog.DataContext;
            viewModel.RadarSettings = dialogViewModel.RadarSettings;

            // 默认显示图片雷达回波
            bool showImageEchoFirst = true;
            foreach (var radarSetting in viewModel.RadarSettings)
            {
                radarSetting.IsEchoDisplayed = showImageEchoFirst;
                radarSetting.IsOpenGlEchoDisplayed = !showImageEchoFirst;

                // TODO: 添加多层图层
                //OpenGlEchoOverlay.IsDisplay = radarSetting.IsOpenGlEchoDisplayed;
                //OpenGlEchoOverlay.Visibility = radarSetting.IsOpenGlEchoDisplayed ? Visibility.Visible : Visibility.Hidden;
            }

            // 抓取 CAT240 网络包
            for (int radarId = 0; radarId < RadarCount; radarId++)
            {
                var radarSetting = viewModel.RadarSettings[radarId];
                viewModel.CaptureCat240NetworkPackage(radarId, radarSetting.RadarIpAddress, radarSetting.RadarPort);
            }
        }

        private void OnRadarChanged(object sender, int radarId, RadarSetting radarSetting)
        {
            InitializeEchoData();
            // TODO: 优化提示消息
            TbStatusInfo.Text = $"Listening radar on {radarSetting.RadarIpAddress}:{radarSetting.RadarPort}.";
        }

        private void OnRadarConnectionStatusChanged(object sender, int radarId, string ip, int port, RadarConnectionStatus status)
        {
            string imgSrc = @"Assets\Disconnected.png";
            switch (status)
            {
                case RadarConnectionStatus.Connected:
                    imgSrc = @"Assets\Connected.png";
                    break;
                case RadarConnectionStatus.Normal:
                    imgSrc = @"Assets\Normal.png";
                    break;
            }

            Dispatcher.Invoke(() =>
            {
                switch (radarId)
                {
                    case 0:
                        ImgRadar1Status.Source = new BitmapImage(new Uri(imgSrc, UriKind.Relative));
                        break;
                    case 1:
                        ImgRadar2Status.Source = new BitmapImage(new Uri(imgSrc, UriKind.Relative));
                        break;
                    case 2:
                        ImgRadar3Status.Source = new BitmapImage(new Uri(imgSrc, UriKind.Relative));
                        break;
                    case 3:
                        ImgRadar4Status.Source = new BitmapImage(new Uri(imgSrc, UriKind.Relative));
                        break;
                    case 4:
                        ImgRadar5Status.Source = new BitmapImage(new Uri(imgSrc, UriKind.Relative));
                        break;
                }
            });
        }

        private void OnCat240SpecChanged(object sender, int radarId, Cat240Spec cat240spec)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var viewModel = GetViewModel();
                    var radarSetting = viewModel.RadarSettings[radarId];

                    RelocateAndScaleRadarEcho(radarId, radarSetting, viewModel.EncScale);
                    RedrawRadarRings(viewModel);

                    // TODO: 疑问点
                    // OpenGlEchoOverlay.RadarOrientation = viewModel.RadarOrientation;
                    // OpenGlEchoOverlay.RadarMaxDistance = viewModel.RadarMaxDistance;
                    // OpenGlEchoOverlay.RealCells = viewModel.CellCount;
                });
            }
            catch (Exception e)
            {
                // TODO: 有没有更好的优雅结束的方法
            }
        }

        private void OnCat240PackageReceived(object sender, int radarId, Cat240DataBlock data)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var viewModel = GetViewModel();

                    // OpenGL 回波图像绘制
                    if (viewModel.IsRadar1OpenGlEchoDisplayed)
                    {
                        // TODO: 疑问点
                        // OpenGlEchoOverlay.RadarOrientation = viewModel.RadarOrientation;
                        // OpenGlEchoOverlay.RadarMaxDistance = viewModel.RadarMaxDistance;
                        // OpenGlEchoOverlay.RealCells = viewModel.CellCount;
                        // ExampleScene.OnReceivedCat240DataBlock(sender, data);
                    }
                });
            }
            catch (Exception e)
            {
                // TODO: 有没有更好的优雅结束的方法
            }
        }
        #endregion

        private void BaseMapView_OnViewpointChanged(object? sender, EventArgs e)
        {
            var viewpoint = BaseMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            if (viewpoint == null)
            {
                return;
            }

            var viewModel = GetViewModel();
            viewModel.RecordEncViewPoint(viewpoint);

            if (viewModel.IsEncLoaded)
            {
                DrawScaleLine();
            }

            RefreshRadarEcho(viewModel);
            RedrawRadarRings(viewModel);
        }

        private void RedrawRadarRings(RadarMonitorViewModel viewModel)
        {
            RadarRingsOverlay.Children.Clear();

            int radarId = 0;
            foreach (var radarSetting in viewModel.RadarSettings)
            {
                if (radarSetting.IsConnected && radarSetting.IsRingsDisplayed)
                {
                    DrawRings(radarId, radarSetting.RadarLongitude, radarSetting.RadarLatitude);
                }

                radarId++;
            }
        }

        private void RefreshRadarEcho(RadarMonitorViewModel viewModel)
        {
            for (int radarId = 0; radarId < RadarCount; radarId++)
            {
                var radarSetting = viewModel.RadarSettings[radarId];
                RelocateAndScaleRadarEcho(radarId, radarSetting, viewModel.EncScale);
            }
        }

        private void RelocateAndScaleRadarEcho(int radarId, RadarSetting radarSetting, double encScale)
        {
            if (!radarSetting.IsConnected)
            {
                return;
            }

            if (radarSetting.IsEchoDisplayed)
            {
                TransformRadarEcho(radarId, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, encScale);
            }

            if (radarSetting.IsOpenGlEchoDisplayed)
            {
                TransformOpenGlRadarEcho(radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, encScale);
            }
        }

        private void BaseMapView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            MapView mapView = (MapView)sender;
            var viewModel = GetViewModel();
            viewModel.EncScale = mapView.MapScale;

            if (viewModel.IsEncLoaded)
            {
                DrawScaleLine();
            }

            RefreshRadarEcho(viewModel);
            RedrawRadarRings(viewModel);
        }

        private void ConfigDisplay_OnClick(object sender, RoutedEventArgs e)
        {
            var configDialog = new DisplayConfigDialog(_echoColor, _isFadingEnabled, _fadingInterval);
            bool? dialogResult = configDialog.ShowDialog();

            if (dialogResult == true)
            {
                var config = (DisplayConfigViewModel)configDialog.DataContext;

                _echoColor = config.ScanlineColor;
                // Change Color
                //for (int i = 0; i < _radar1EchoData.Length; i += 4)
                //{
                //    byte alpha = _radar1EchoData[i + 3];
                //    if (alpha == 0)
                //    {
                //        continue;
                //    }

                //    _radar1EchoData[i + 0] = _echoColor.B;
                //    _radar1EchoData[i + 1] = _echoColor.G;
                //    _radar1EchoData[i + 2] = _echoColor.R;
                //}

                _isFadingEnabled = config.IsFadingEnabled;
                _fadingInterval = config.FadingInterval;
            }
        }

        #region Display Control
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

        private void CbRadar1Echo_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                var viewModel = GetViewModel();
                var radarSetting = viewModel.RadarSettings[0];
                TransformRadarEcho(0, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, viewModel.EncScale);
                Radar1EchoImage.Visibility = Visibility.Visible;
            }
            else
            {
                Radar1EchoImage.Visibility = Visibility.Hidden;
            }
        }

        private void CbRadar1Rings_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayRingsCheckBox = (CheckBox)sender;

            var viewModel = GetViewModel();
            if (displayRingsCheckBox.IsChecked.Value)
            {
                viewModel.RadarSettings[0].IsRingsDisplayed = true;
            }
            else
            {
                viewModel.RadarSettings[0].IsRingsDisplayed = false;
            }

            RedrawRadarRings(viewModel);
        }



        private void CbRadar2Echo_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                var viewModel = GetViewModel();
                var radarSetting = viewModel.RadarSettings[1];
                TransformRadarEcho(1, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, viewModel.EncScale);
                Radar2EchoImage.Visibility = Visibility.Visible;
            }
            else
            {
                Radar2EchoImage.Visibility = Visibility.Hidden;
            }
        }

        private void CbRadar2Rings_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayRingsCheckBox = (CheckBox)sender;

            var viewModel = GetViewModel();
            if (displayRingsCheckBox.IsChecked.Value)
            {
                viewModel.RadarSettings[1].IsRingsDisplayed = true;
            }
            else
            {
                viewModel.RadarSettings[1].IsRingsDisplayed = false;
            }

            RedrawRadarRings(viewModel);
        }

        private void CbRadar3Echo_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                var viewModel = GetViewModel();
                var radarSetting = viewModel.RadarSettings[2];
                TransformRadarEcho(2, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, viewModel.EncScale);
                Radar3EchoImage.Visibility = Visibility.Visible;
            }
            else
            {
                Radar3EchoImage.Visibility = Visibility.Hidden;
            }
        }

        private void CbRadar3Rings_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayRingsCheckBox = (CheckBox)sender;

            var viewModel = GetViewModel();
            if (displayRingsCheckBox.IsChecked.Value)
            {
                viewModel.RadarSettings[2].IsRingsDisplayed = true;
            }
            else
            {
                viewModel.RadarSettings[2].IsRingsDisplayed = false;
            }

            RedrawRadarRings(viewModel);
        }

        private void CbRadar4Echo_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                var viewModel = GetViewModel();
                var radarSetting = viewModel.RadarSettings[3];
                TransformRadarEcho(3, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, viewModel.EncScale);
                Radar4EchoImage.Visibility = Visibility.Visible;
            }
            else
            {
                Radar4EchoImage.Visibility = Visibility.Hidden;
            }
        }

        private void CbRadar4Rings_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayRingsCheckBox = (CheckBox)sender;

            var viewModel = GetViewModel();
            if (displayRingsCheckBox.IsChecked.Value)
            {
                viewModel.RadarSettings[3].IsRingsDisplayed = true;
            }
            else
            {
                viewModel.RadarSettings[3].IsRingsDisplayed = false;
            }

            RedrawRadarRings(viewModel);
        }

        private void CbRadar5Echo_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                var viewModel = GetViewModel();
                var radarSetting = viewModel.RadarSettings[4];
                TransformRadarEcho(4, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, viewModel.EncScale);
                Radar5EchoImage.Visibility = Visibility.Visible;

            }
            else
            {
                Radar5EchoImage.Visibility = Visibility.Hidden;
            }
        }

        private void CbRadar5Rings_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayRingsCheckBox = (CheckBox)sender;

            var viewModel = GetViewModel();
            if (displayRingsCheckBox.IsChecked.Value)
            {
                viewModel.RadarSettings[4].IsRingsDisplayed = true;
            }
            else
            {
                viewModel.RadarSettings[4].IsRingsDisplayed = false;
            }

            RedrawRadarRings(viewModel);
        }

        private void DisplayOpenGlEcho_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayOpenGlEchoCheckBox = (CheckBox)sender;
            var viewModel = GetViewModel();

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
        #endregion

        #region Map Operations
        private void GotoViewPoint(Viewpoint viewpoint)
        {
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
                    var viewModel = GetViewModel();
                    var encSetting = viewModel.Configuration.EncSetting;
                    if ((encSetting.EncLongitude != 0.0) && (encSetting.EncLatitude != 0.0))
                    {
                        newCenterPoint = new MapPoint(encSetting.EncLongitude, encSetting.EncLatitude, SpatialReferences.Wgs84);
                        scale = encSetting.EncScale;
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

        private void DrawRings(int radarId, double longitude, double latitude)
        {
            //Canvas ringsOverlay = _ringsOverlays[radarId];
            Canvas ringsOverlay = RadarRingsOverlay;

            //ringsOverlay.Children.Clear();

            Brush ringBrush = new SolidColorBrush(Colors.Chartreuse);   // 距离环的颜色
            DoubleCollection dashArray = new DoubleCollection(new double[] { 2, 4 });
            int ringFontSize = 12;

            // 定义雷达回波的圆心坐标
            var point = BaseMapView.LocationToScreen(new MapPoint(longitude, latitude, SpatialReferences.Wgs84));
            double radarX = point.X;
            double radarY = point.Y;

            double maxDistance = Math.Max(ringsOverlay.ActualWidth, ringsOverlay.ActualHeight) / 2;

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
            ringsOverlay.Children.Add(center);

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
                ringsOverlay.Children.Add(circle);

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
                ringsOverlay.Children.Add(distanceText);
            }
        }

        private void TransformRadarEcho(int radarId, double radarLongitude, double radarLatitude, double radarOrientation, double radarMaxDistance, double encScale)
        {
            if ((radarLongitude == 0) || (radarLatitude == 0) || (encScale == 0))
            {
                return;
            }

            double kmWith1Cm = (encScale / 100000.0);
            double kmWith1px = kmWith1Cm / _dpcX;

            var imageOverlay = _echoImageOverlays[radarId];

            var echoOverlay = RadarEchoOverlay;
            //echoOverlay.Children.Clear();
            //echoOverlay.Children.Add(imageOverlay);

            var size = 2 * radarMaxDistance / kmWith1px;

            imageOverlay.Width = size;
            imageOverlay.Height = size;

            var radarPoint = BaseMapView.LocationToScreen(new MapPoint(radarLongitude, radarLatitude, SpatialReferences.Wgs84));

            Canvas.SetLeft(imageOverlay, radarPoint.X - size / 2.0);
            Canvas.SetTop(imageOverlay, radarPoint.Y - size / 2.0);
        }

        private void TransformOpenGlRadarEcho(double radarLongitude, double radarLatitude, double radarOrientation, double radarMaxDistance, double encScale)
        {
            if ((radarLongitude == 0) || (radarLatitude == 0) || (encScale == 0))
            {
                return;
            }

            double kmWith1Cm = (encScale / 100000.0);
            double kmWith1Px = kmWith1Cm / _dpcX;
            double kmWith1Lon = 111.32;

            int uiWidth = (int)OpenGlEchoOverlay.ActualWidth;
            int uiHeight = (int)OpenGlEchoOverlay.ActualHeight;
            float mapWidth = (float)(uiWidth * kmWith1Px);
            float mapHeight = (float)(uiHeight * kmWith1Px);

            Viewpoint center = BaseMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            MapPoint centerPoint = center.TargetGeometry as MapPoint;

            double xOffset = radarLongitude - centerPoint.X;
            double yOffset = radarLatitude - centerPoint.Y;

            float mapWidthOffCenter = (float)(xOffset * kmWith1Lon);
            float mapHeightOffCenter = (float)(yOffset * kmWith1Lon);

            OpenGlEchoOverlay.UIWidth = uiWidth;
            OpenGlEchoOverlay.UIHeight = uiHeight;
            OpenGlEchoOverlay.MapWidth = mapWidth;
            OpenGlEchoOverlay.MapHeight = mapHeight;
            OpenGlEchoOverlay.MapWidthOffCenter = mapWidthOffCenter;
            OpenGlEchoOverlay.MapHeightOffCenter = mapHeightOffCenter;
            OpenGlEchoOverlay.RadarOrientation = radarOrientation;
        }
        #endregion

        #region Preset Locations
        private void BtnPresetLocation1_OnClick(object sender, RoutedEventArgs e)
        {
            var radarSetting = GetViewModel().RadarSettings[0];
            var mapPoint = new MapPoint(radarSetting.RadarLongitude, radarSetting.RadarLatitude, SpatialReferences.Wgs84);
            var viewPoint = new Viewpoint(mapPoint, radarSetting.RadarScale);
            GotoViewPoint(viewPoint);
        }

        private void BtnPresetLocation2_OnClick(object sender, RoutedEventArgs e)
        {
            var radarSetting = GetViewModel().RadarSettings[1];
            var mapPoint = new MapPoint(radarSetting.RadarLongitude, radarSetting.RadarLatitude, SpatialReferences.Wgs84);
            var viewPoint = new Viewpoint(mapPoint, radarSetting.RadarScale);
            GotoViewPoint(viewPoint);
        }

        private void BtnPresetLocation3_OnClick(object sender, RoutedEventArgs e)
        {
            var radarSetting = GetViewModel().RadarSettings[2];
            var mapPoint = new MapPoint(radarSetting.RadarLongitude, radarSetting.RadarLatitude, SpatialReferences.Wgs84);
            var viewPoint = new Viewpoint(mapPoint, radarSetting.RadarScale);
            GotoViewPoint(viewPoint);
        }

        private void BtnPresetLocation4_OnClick(object sender, RoutedEventArgs e)
        {
            var radarSetting = GetViewModel().RadarSettings[3];
            var mapPoint = new MapPoint(radarSetting.RadarLongitude, radarSetting.RadarLatitude, SpatialReferences.Wgs84);
            var viewPoint = new Viewpoint(mapPoint, radarSetting.RadarScale);
            GotoViewPoint(viewPoint);
        }

        private void BtnPresetLocation5_OnClick(object sender, RoutedEventArgs e)
        {
            var radarSetting = GetViewModel().RadarSettings[4];
            var mapPoint = new MapPoint(radarSetting.RadarLongitude, radarSetting.RadarLatitude, SpatialReferences.Wgs84);
            var viewPoint = new Viewpoint(mapPoint, radarSetting.RadarScale);
            GotoViewPoint(viewPoint);
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

        #region Windows EventHandler
        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            var viewModel = GetViewModel();
            viewModel.DisposeCat240Parser();
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
            //var viewModel = GetViewModel();
            //if ((viewModel.EncLongitude != 0.0) && (viewModel.EncLatitude != 0.0))
            //{
            //    var distance = CalculateDistance(viewModel.EncLongitude, viewModel.EncLatitude, location.X, location.Y);
            //    CursorDistance.Content = "D2R:   " + distance.ToString("F2") + " KM";

            //    var azimuth = CalculateAzimuth(viewModel.EncLongitude, viewModel.EncLatitude,location.X, location.Y);
            //    CursorAzimuth.Content = "A2R:   " + azimuth.ToString("F2") + "°";
            //}
        }
        #endregion

        #region Utilities
        private RadarMonitorViewModel GetViewModel()
        {
            return (RadarMonitorViewModel)DataContext;
        }

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
        #endregion
    }
}
