﻿using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Hydrography;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using RadarMonitor.Model;
using RadarMonitor.ViewModel;
using Silk.WPF.OpenGL;
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
        private const int RadarCount = 5;

        private bool _showInKm = false;
        private const double KmPerNm = 1.852;

        #region Radar Display Settings
        // Radar1 Display Setting
        private Color _radar1EchoColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isRadar1FadingEnabled = true;
        private int _radar1FadingInterval = 7;
        private double _radar1EchoThreshold = 0;
        private double _radar1EchoRadius = 1;
        private double _radar1MaxDistance = 0;

        // Radar2 Display Setting
        private Color _radar2EchoColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isRadar2FadingEnabled = true;
        private int _radar2FadingInterval = 7;
        private double _radar2EchoThreshold = 0;
        private double _radar2EchoRadius = 1;
        private double _radar2MaxDistance = 0;

        // Radar3 Display Setting
        private Color _radar3EchoColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isRadar3FadingEnabled = true;
        private int _radar3FadingInterval = 7;
        private double _radar3EchoThreshold = 0;
        private double _radar3EchoRadius = 1;
        private double _radar3MaxDistance = 0;

        // Radar4 Display Setting
        private Color _radar4EchoColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isRadar4FadingEnabled = true;
        private int _radar4FadingInterval = 7;
        private double _radar4EchoThreshold = 0;
        private double _radar4EchoRadius = 1;
        private double _radar4MaxDistance = 0;

        // Radar5 Display Setting
        private Color _radar5EchoColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isRadar5FadingEnabled = true;
        private int _radar5FadingInterval = 7;
        private double _radar5EchoThreshold = 0;
        private double _radar5EchoRadius = 1;
        private double _radar5MaxDistance = 0;

        // Radar Display Settings
        private List<Color> _radarEchoColors;
        private List<bool> _isRadarFadingEnabledFlags;
        private List<int> _radarFadingIntervals;
        private List<double> _radarEchoThresholds;
        private List<double> _radarEchoRadiuses;
        private List<double> _radarMaxDistances;
        #endregion
        
        private readonly bool _encDetailDisplayFlag = false;

        public RadarSettingsViewModel DialogViewModel { get; set; }

        private Dictionary<int, RadarInfoModel> _radarInfos = new();

        public MainWindow()
        {
            InitializeComponent();

            // 初始化显示单位
            InitializeDisplayMetric();

            // 初始化ViewModel及注册事件
            InitializeViewModel();

            // 初始化 ArcGIS 地图控件
            InitializeMapView();

            // 初始化雷达显示设置
            InitializeRadarDisplaySettings();

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
            viewModel.OnCat240PackageReceivedOpenGLEvent += OnCat240PackageReceived;
            DataContext = viewModel;
        }

        private void InitializeMapView()
        {
            BaseMapView.Map = new Map(SpatialReferences.Wgs84);
            BaseMapView.IsAttributionTextVisible = false;

            #region Enc configuration
            // 海图显示配置：不显示 海床，深度点，地理名称等
            EncEnvironmentSettings.Default.DisplaySettings.MarinerSettings.ColorScheme = EncColorScheme.Day;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.AllIsolatedDangers = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ArchipelagicSeaLanes = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BoundariesAndLimits = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BuoysBeaconsAidsToNavigation = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BuoysBeaconsStructures = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ChartScaleBoundaries = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.DepthContours = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.DryingLine = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Lights = false;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.MagneticVariation = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.OtherMiscellaneous = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ProhibitedAndRestrictedAreas = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Seabed = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ShipsRoutingSystemsAndFerryRoutes = false;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SpotSoundings = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.StandardMiscellaneous = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SubmarineCablesAndPipelines = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Tidal = _encDetailDisplayFlag;

            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.BerthNumber = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.CurrentVelocity = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.GeographicNames = false;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.HeightOfIsletOrLandFeature = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.ImportantText = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.LightDescription = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.MagneticVariationAndSweptDepth = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NamesForPositionReporting = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NatureOfSeabed = _encDetailDisplayFlag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NoteOnChartData = _encDetailDisplayFlag;
            #endregion
        }
        
        private void InitializeRadarDisplaySettings()
        {
            _radarEchoColors = new()
            {
                _radar1EchoColor, _radar2EchoColor, _radar3EchoColor,
                _radar4EchoColor, _radar5EchoColor
            };

            _isRadarFadingEnabledFlags = new()
            {
                _isRadar1FadingEnabled, _isRadar2FadingEnabled, _isRadar3FadingEnabled,
                _isRadar4FadingEnabled, _isRadar5FadingEnabled
            };

            _radarFadingIntervals = new()
            {
                _radar1FadingInterval, _radar2FadingInterval, _radar3FadingInterval,
                _radar4FadingInterval, _radar5FadingInterval
            };

            _radarEchoThresholds = new()
            {
                _radar1EchoThreshold, _radar2EchoThreshold, _radar3EchoThreshold,
                _radar4EchoThreshold, _radar5EchoThreshold
            };

            _radarEchoRadiuses = new()
            {
                _radar1EchoRadius, _radar2EchoRadius, _radar3EchoRadius,
                _radar4EchoRadius, _radar5EchoRadius
            };

            _radarMaxDistances = new()
            {
                _radar1MaxDistance, _radar2MaxDistance, _radar3MaxDistance,
                _radar4MaxDistance, _radar5MaxDistance
            };
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

                    DrawScaleLine();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
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

            // 以下代码可能会导致海图加载异常，所以注释掉
            //Envelope fullExtent = GeometryEngine.CombineExtents(dataSetExtents); 
            //await BaseMapView.SetViewpointAsync(new Viewpoint(fullExtent));

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
            //await BaseMapView.SetViewpointAsync(new Viewpoint(fullExtent));

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
            DialogViewModel = (RadarSettingsViewModel)radarDialog.DataContext;
            bool? dialogResult = radarDialog.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }

            // 获取雷达静态信息
            var dialogViewModel = (RadarSettingsViewModel)radarDialog.DataContext;
            viewModel.RadarSettings = dialogViewModel.RadarSettings;

            // 默认显示图片雷达回波
            bool showImageEchoFirst = false;
            //foreach (var radarSetting in viewModel.RadarSettings)
            //{
            //    radarSetting.IsEchoDisplayed = showImageEchoFirst;
            //    radarSetting.IsOpenGlEchoDisplayed = !showImageEchoFirst;

            //}
            viewModel.IsOpenGlEchoDisplayed = true;
            OpenGlEchoOverlay.IsDisplay = true;
            OpenGlEchoOverlay.Visibility = Visibility.Visible;
            for (int i = 0; i < viewModel.RadarSettings.Count; i++)
            {
                var radarSetting= viewModel.RadarSettings[i];
                radarSetting.IsEchoDisplayed = showImageEchoFirst;
                radarSetting.IsOpenGlEchoDisplayed = !showImageEchoFirst;

                // CreateUpdateRadar
                CreateUpdateRadarInfoBase(i, radarSetting);
                TransformOpenGlRadarEcho(i, radarSetting.DisplayScaleRatio, viewModel.EncScale);
            }

            // 抓取 CAT240 网络包
            for (int radarId = 0; radarId < RadarCount; radarId++)
            {
                var radarSetting = viewModel.RadarSettings[radarId];
                viewModel.CaptureCat240NetworkPackage(radarId, radarSetting.RadarIpAddress, radarSetting.RadarPort, radarSetting.LocalIp);
            }

            OpenGlEchoOverlay.OnSessionChanged();

            DrawScaleLine();
        }

        private void OnRadarChanged(object sender, int radarId, RadarSetting radarSetting)
        {
            //InitializeEchoData();
            // TODO: 优化提示消息
            TbStatusInfo.Text = $"Listening radar on {radarSetting.RadarIpAddress}:{radarSetting.RadarPort}.";
        }

        private void OnRadarConnectionStatusChanged(object sender, int radarId, string ip, int port, RadarConnectionStatus status)
        {
            Dispatcher.Invoke(() =>
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

                    // for display config dialog
                    _radarEchoRadiuses[radarId] = cat240spec.MaxDistance;
                    _radarMaxDistances[radarId] = cat240spec.MaxDistance;

                    // opengl
                    RadarInfoModel radarInfo = _radarInfos[radarId];

                    radarInfo.RealCells = (int)cat240spec.ValidCellsInDataBlock;
                    radarInfo.RadarMaxDistance = cat240spec.MaxDistance;

                    TransformOpenGlRadarEcho(radarId, radarSetting.DisplayScaleRatio, viewModel.EncScale);
                    RedrawRadarRings(viewModel);
                });
            }
            catch (Exception ex)
            {
                // TODO: 有没有更好的优雅结束的方法
            }
        }

        private void OnCat240PackageReceived(object sender, RadarDataReceivedEventArgs e)
        {
            try
            {
                OpenGlEchoOverlay.OnReceivedRadarData(sender, e);
            }
            catch (Exception ex)
            {
                // TODO: 有没有更好的优雅结束的方法
            }
        }
        #endregion

        #region Map EventHandler
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

            //if (radarSetting.IsEchoDisplayed)
            //{
            //    TransformRadarEcho(radarId, radarSetting.RadarLongitude, radarSetting.RadarLatitude,
            //        radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, encScale);
            //}

            if (radarSetting.IsOpenGlEchoDisplayed)
            {
                //TransformOpenGlRadarEcho(radarSetting.RadarLongitude, radarSetting.RadarLatitude,
                //    radarSetting.RadarOrientation, radarSetting.RadarMaxDistance, encScale);
                TransformOpenGlRadarEcho(radarId, radarSetting.DisplayScaleRatio, encScale);
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
        #endregion

        #region Layer Display Control
        private void DisplayEnc_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEncCheckBox = (CheckBox)sender;

            if (displayEncCheckBox.IsChecked.Value)
            {
                BaseMapView.Visibility = Visibility.Visible;
                ScaleOverlay.Visibility = Visibility.Visible;
                OpenGlEchoOverlay.ControlOpacity = 0.8;
            }
            else
            {
                if (GetViewModel().IsRadarConnected)
                {
                    OpenGlEchoOverlay.ControlOpacity = 1.0;
                }
                else
                {
                    BaseMapView.Visibility = Visibility.Hidden;
                    ScaleOverlay.Visibility = Visibility.Hidden;
                }
            }
        }

        private void CbRadar1Echo_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                _radarInfos[0].IsDisplay=true;
                OpenGlEchoOverlay.OnDisplayed(0, _radarInfos[0].IsDisplay);
            }
            else
            {
                _radarInfos[0].IsDisplay = false;
                OpenGlEchoOverlay.OnDisplayed(0, _radarInfos[0].IsDisplay);

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
                _radarInfos[1].IsDisplay = true;
                OpenGlEchoOverlay.OnDisplayed(1, _radarInfos[1].IsDisplay);

            }
            else
            {
                _radarInfos[1].IsDisplay = false;
                OpenGlEchoOverlay.OnDisplayed(1, _radarInfos[1].IsDisplay);

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
                _radarInfos[2].IsDisplay = true;
                OpenGlEchoOverlay.OnDisplayed(2, _radarInfos[2].IsDisplay);
            }
            else
            {
                _radarInfos[2].IsDisplay = false;
                OpenGlEchoOverlay.OnDisplayed(2, _radarInfos[2].IsDisplay);


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
                _radarInfos[3].IsDisplay = true;
                OpenGlEchoOverlay.OnDisplayed(3, _radarInfos[3].IsDisplay);
            }
            else
            {
                _radarInfos[3].IsDisplay = false;
                OpenGlEchoOverlay.OnDisplayed(3, _radarInfos[3].IsDisplay);


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
                _radarInfos[4].IsDisplay = true;
                OpenGlEchoOverlay.OnDisplayed(4, _radarInfos[4].IsDisplay);
            }
            else
            {
                _radarInfos[4].IsDisplay = false;
                OpenGlEchoOverlay.OnDisplayed(4, _radarInfos[4].IsDisplay);

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
            //var viewModel = GetViewModel();

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

        #region Radar Display Control
        private void BtnRadar1Config_OnClick(object sender, RoutedEventArgs e)
        {
            int radarId = 0;
            ConfigRadarDisplay(radarId);
        }

        private void BtnRadar2Config_OnClick(object sender, RoutedEventArgs e)
        {
            int radarId = 1;
            ConfigRadarDisplay(radarId);
        }

        private void BtnRadar3Config_OnClick(object sender, RoutedEventArgs e)
        {
            int radarId = 2;
            ConfigRadarDisplay(radarId);
        }

        private void BtnRadar4Config_OnClick(object sender, RoutedEventArgs e)
        {
            int radarId = 3;
            ConfigRadarDisplay(radarId);
        }

        private void BtnRadar5Config_OnClick(object sender, RoutedEventArgs e)
        {
            int radarId = 4;
            ConfigRadarDisplay(radarId);
        }

        private void ConfigRadarDisplay(int radarId)
        {
            Color prevEchoColor = _radarEchoColors[radarId];
            bool prevFadingEnabled = _isRadarFadingEnabledFlags[radarId];
            int prevFadingInterval = _radarFadingIntervals[radarId];
            double prevEchoThreshold = _radarEchoThresholds[radarId];
            double prevEchoRadius = _radarEchoRadiuses[radarId];
            double prevEchoMaxDistance = _showInKm ? _radarMaxDistances[radarId] : _radarMaxDistances[radarId] / KmPerNm;

            var configDialog = new DisplayConfigDialog(radarId, prevEchoColor, prevFadingEnabled, prevFadingInterval,
                prevEchoThreshold, prevEchoRadius, prevEchoMaxDistance, _showInKm);

            var config = (DisplayConfigViewModel)configDialog.DataContext;
            config.OnEchoColorChanged += ConfigOnOnEchoColorChanged;
            config.OnFadingEnabledChanged += ConfigOnOnFadingEnabledChanged;
            config.OnFadingIntervalChanged += ConfigOnOnFadingIntervalChanged;
            config.OnEchoThresholdChanged += ConfigOnOnEchoThresholdChanged;
            config.OnEchoRadiusChanged += ConfigOnOnEchoRadiusChanged;

            bool? dialogResult = configDialog.ShowDialog();

            var radar = _radarInfos[radarId];
            if (dialogResult == true)
            {
                _radarEchoColors[radarId] = config.ScanlineColor;
                _isRadarFadingEnabledFlags[radarId] = config.IsFadingEnabled;
                _radarFadingIntervals[radarId] = config.FadingInterval;
                _radarEchoThresholds[radarId] = config.EchoThreshold;
                _radarEchoRadiuses[radarId] = config.EchoRadius;

                if (radar != null)
                {
                    radar.ScanlineColor = config.ScanlineColor;
                    radar.IsFadingEnabled = config.IsFadingEnabled;
                    radar.FadingInterval = config.FadingInterval;
                    radar.EchoThreshold = (float)config.EchoThreshold;
                    radar.EchoRadius = (float)(config.EchoRadius / _radarMaxDistances[radarId]);
                    OpenGlEchoOverlay.OnConfigChanged(radar);
                }
            }
            else
            {
                if (radar != null)
                {
                    radar.ScanlineColor = prevEchoColor;
                    radar.IsFadingEnabled = prevFadingEnabled;
                    radar.FadingInterval = prevFadingInterval;
                    radar.EchoThreshold = (float)prevEchoThreshold;
                    radar.EchoRadius = (float)(prevEchoRadius / _radarMaxDistances[radarId]);
                    OpenGlEchoOverlay.OnConfigChanged(radar);
                }
            }
        }

        private void ConfigOnOnEchoColorChanged(object sender, int radarId, Color scanlineColor)
        {
            var radar = _radarInfos[radarId];
            if (radar != null)
            {
                radar.ScanlineColor = scanlineColor;
                OpenGlEchoOverlay.OnConfigChanged(radar);
            }
        }

        private void ConfigOnOnFadingEnabledChanged(object sender, int radarId, bool isFadingEnabled)
        {
            var radar = _radarInfos[radarId];
            if (radar != null)
            {
                radar.IsFadingEnabled = isFadingEnabled;
                OpenGlEchoOverlay.OnConfigChanged(radar);
            }
        }

        private void ConfigOnOnFadingIntervalChanged(object sender, int radarId, int fadingInterval)
        {
            var radar = _radarInfos[radarId];
            if (radar != null)
            {
                radar.FadingInterval = fadingInterval;
                OpenGlEchoOverlay.OnConfigChanged(radar);
            }
        }

        private void ConfigOnOnEchoThresholdChanged(object sender, int radarId, double echoThreshold)
        {
            var radar = _radarInfos[radarId];
            if (radar != null)
            {
                radar.EchoThreshold = (float)echoThreshold;
                OpenGlEchoOverlay.OnConfigChanged(radar);
            }
        }

        private void ConfigOnOnEchoRadiusChanged(object sender, int radarId, double echoRadius)
        {
            var radar = _radarInfos[radarId];
            if (radar != null)
            {
                radar.EchoRadius = (float)(echoRadius / _radarMaxDistances[radarId]);
                OpenGlEchoOverlay.OnConfigChanged(radar);
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
            double nmWith1Cm = kmWith1Cm / KmPerNm;

            var viewModel = GetViewModel();

            Brush scaleBrush = new SolidColorBrush(Colors.LimeGreen);
            if (!viewModel.IsRadarConnected)
            {
                scaleBrush = new SolidColorBrush(Colors.Black);
            }
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
                string distance = String.Empty;
                if (_showInKm)
                {
                    distance = (end * kmWith1Cm).ToString("F1");
                }
                else
                {
                    distance = (end * nmWith1Cm).ToString("F1");
                }

                TextBlock TailLabel = new TextBlock
                {
                    Text = distance,
                    Foreground = scaleBrush,
                    FontSize = labelFontSize,
                    Margin = new Thickness(end * _dpcX - labelXOffset, canvasHeight / 2 + labelYOffset, 0, 0)
                };
                ScaleOverlay.Children.Add(TailLabel);
            }

            // 距离单位尾标
            string unitLabel = string.Empty;
            if (_showInKm)
            {
                unitLabel = "(KM)";
            }
            else
            {
                unitLabel = "(NM)";
            }

            TextBlock kmLabel = new TextBlock
            {
                Text = unitLabel,
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
            int ringFontSize = 10;

            // 定义雷达回波的圆心坐标
            var point = BaseMapView.LocationToScreen(new MapPoint(longitude, latitude, SpatialReferences.Wgs84));
            double radarX = point.X;
            double radarY = point.Y;

            var viewModel = GetViewModel();
            var radarSetting = viewModel.RadarSettings[radarId];
            double kmWith1Cm = (viewModel.EncScale / 100000.0);
            double kmWith1Px = kmWith1Cm / _dpcX;
            double maxDistance = 1.2 * radarSetting.RadarMaxDistance / kmWith1Px;

            double screenMaxDistance = Math.Max(ringsOverlay.ActualWidth, ringsOverlay.ActualHeight) / 2;

            maxDistance = Math.Min(maxDistance, screenMaxDistance);

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
                double radiusInNm = radiusInKm / KmPerNm;
                ringOffset += ringStep;

                if (radiusInKm == 0)
                {
                    continue;   // 不显示 0KM
                }

                // 右标尺环文字
                TextBlock distanceTextRight = new TextBlock();
                if (_showInKm)
                {
                    distanceTextRight.Text = radiusInKm.ToString("F1") + "KM";
                }
                else
                {
                    distanceTextRight.Text = radiusInNm.ToString("F1") + "NM";
                }
                
                distanceTextRight.Foreground = ringBrush;
                distanceTextRight.FontSize = ringFontSize;

                Canvas.SetLeft(distanceTextRight, radarX + radius + 2);
                Canvas.SetTop(distanceTextRight, radarY);

                ringsOverlay.Children.Add(distanceTextRight);

                // 左标尺环文字
                TextBlock distanceTextLeft = new TextBlock();
                if (_showInKm)
                {
                    distanceTextLeft.Text = radiusInKm.ToString("F1") + "KM";
                }
                else
                {
                    distanceTextLeft.Text = radiusInNm.ToString("F1") + "NM";
                }

                distanceTextLeft.Foreground = ringBrush;
                distanceTextLeft.FontSize = ringFontSize;

                Canvas.SetLeft(distanceTextLeft, radarX - radius - 40);
                Canvas.SetTop(distanceTextLeft, radarY);

                ringsOverlay.Children.Add(distanceTextLeft);
            }
        }

        private void TransformOpenGlRadarEcho(int radarId, double displayRatio, double encScale)
        {
            RadarInfoModel radarInfo = _radarInfos[radarId];
            if ((radarInfo.Longitude == 0) || (radarInfo.Latitude == 0) || (encScale == 0))
            {
                return;
            }

            double kmWith1Cm = (encScale / 100000.0);
            double kmWith1Px = kmWith1Cm / _dpcX;
            double kmWith1Lon = 111.32;

            //double adjustRatio = 1.4;   // magic number
            double adjustRatio = displayRatio;

            int uiWidth = (int)OpenGlEchoOverlay.ActualWidth;
            int uiHeight = (int)OpenGlEchoOverlay.ActualHeight;
            float mapWidth = (float)(uiWidth * kmWith1Px * adjustRatio);
            float mapHeight = (float)(uiHeight * kmWith1Px * adjustRatio);

            Viewpoint center = BaseMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            MapPoint centerPoint = center.TargetGeometry as MapPoint;

            double xOffset = radarInfo.Longitude - centerPoint.X;
            double yOffset = radarInfo.Latitude - centerPoint.Y;

            float mapWidthOffCenter = (float)(xOffset * kmWith1Lon * adjustRatio);
            float mapHeightOffCenter = (float)(yOffset * kmWith1Lon * adjustRatio);

            radarInfo.UIWidth = uiWidth;
            radarInfo.UIHeight = uiHeight;
            radarInfo.MapWidth = mapWidth;
            radarInfo.MapHeight = mapHeight;
            radarInfo.MapWidthOffCenter = mapWidthOffCenter;
            radarInfo.MapHeightOffCenter = mapHeightOffCenter;

            OpenGlEchoOverlay.CreateUpdateRadar(radarInfo);
        }

        public void CreateUpdateRadarInfoBase(int radarId, RadarSetting setting)
        {
            RadarInfoModel radarInfo;
            if (_radarInfos.ContainsKey(radarId))
            {
                radarInfo = _radarInfos[radarId];
            }
            else
            {
                radarInfo = new RadarInfoModel(radarId);
                _radarInfos.Add(radarId, radarInfo);

            }

            radarInfo.Latitude = setting.RadarLatitude;
            radarInfo.Longitude = setting.RadarLongitude;
            radarInfo.RadarOrientation = setting.RadarOrientation;
            radarInfo.IsDisplay = setting.IsOpenGlEchoDisplayed;

            radarInfo.ScanlineColor = _radarEchoColors[radarId];
            radarInfo.IsFadingEnabled = _isRadarFadingEnabledFlags[radarId];
            radarInfo.FadingInterval = _radarFadingIntervals[radarId];
            radarInfo.EchoThreshold = (float)_radarEchoThresholds[radarId];
            radarInfo.EchoRadius = (float)_radarEchoRadiuses[radarId];
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

        private void RbNm_OnChecked(object sender, RoutedEventArgs e)
        {
            var viewModel = GetViewModel();
            if (viewModel == null || !viewModel.IsEncLoaded)
            {
                return;
            }

            _showInKm = false;

            DrawScaleLine();
            RedrawRadarRings(viewModel);
        }

        private void RbKm_OnChecked(object sender, RoutedEventArgs e)
        {
            var viewModel = GetViewModel();
            if (viewModel == null || !viewModel.IsEncLoaded)
            {
                return;
            }

            _showInKm = true;

            DrawScaleLine();
            RedrawRadarRings(viewModel);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Trace.TraceInformation("OnActivated");
            OpenGlEchoOverlay.OnSessionChanged();
        }
    }
}