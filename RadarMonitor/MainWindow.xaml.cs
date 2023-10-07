using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Hydrography;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.Win32;
using RadarMonitor.Model;
using RadarMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Window = System.Windows.Window;

namespace RadarMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int ImageSize = RadarMonitorViewModel.CartesianSzie;

        private double _dpiX;
        private double _dpiY;
        private double _dpcX;
        private double _dpcY;

        private byte[] _echoData;
        private WriteableBitmap _bitmap = new WriteableBitmap(ImageSize, ImageSize,96, 96, PixelFormats.Bgra32, null);

        private DispatcherTimer _timer = new DispatcherTimer();
        private const int RefreshIntervalMs = 100;

        private Color _scanlineColor = DisplayConfigDialog.DefaultImageEchoColor;
        private bool _isFadingEnabled = false;
        private int _fadingInterval = 5;

        private bool _flag = false;

        public MainWindow()
        {
            InitializeComponent();

            InitializeMapView();

            int size = RadarMonitorViewModel.CartesianSzie;
            _echoData = new byte[size * size * 4];

            var viewModel = new RadarMonitorViewModel();
            viewModel.OnPolarLineUpdated += ViewModelOnPolarLineUpdated;

            DataContext = viewModel;

            _timer.Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var viewModel = (RadarMonitorViewModel)DataContext;
            if (!viewModel.IsEchoDisplayed)
            {
                return;
            }

            int stride = RadarMonitorViewModel.CartesianSzie * 4;
            _bitmap.WritePixels(
                new Int32Rect(0, 0, ImageSize, ImageSize), _echoData, stride, 0);
            EchoImageOverlay.Source = _bitmap;
            EchoImageOverlay.InvalidateVisual();

            // Fading
            if (_isFadingEnabled)
            {
                byte fadingStep = (byte)(255 / _fadingInterval / (1000 / RefreshIntervalMs));
                for (int i = 0; i < _echoData.Length; i += 4)
                {
                    byte alpha = _echoData[i + 3];
                    if (alpha == 0)
                    {
                        continue;
                    }

                    _echoData[i + 3] = (byte)Math.Max((alpha - fadingStep), 0);
                }
            }

            //Mat mat = new Mat(ImageSize, ImageSize, MatType.CV_8UC4, _echoData);
            //mat.SaveImage("temp_gui.png");
        }

        private void ViewModelOnPolarLineUpdated(object sender, List<Tuple<int, int, int>> updatedPixels)
        {
            int stride = RadarMonitorViewModel.CartesianSzie * 4;

            foreach (var pixel in updatedPixels)
            {
                byte r = _scanlineColor.R;
                byte g = _scanlineColor.G;
                byte b = _scanlineColor.B;
                byte a = (byte)pixel.Item3;

                int x = pixel.Item1;
                int y = pixel.Item2;
                int index = y * stride + x * 4;

                _echoData[index + 0] = b;        // Blue
                _echoData[index + 1] = g;        // Green
                _echoData[index + 2] = r;        // Red
                _echoData[index + 3] = a;        // Alpha
            }
        }

        private void InitializeMapView()
        {
            BaseMapView.Map = new Map();
            BaseMapView.IsAttributionTextVisible = false;

            #region Enc configuration
            // 海图显示配置：不显示 海床，深度点，地理名称
            _flag = false;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.AllIsolatedDangers = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ArchipelagicSeaLanes = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BoundariesAndLimits = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BuoysBeaconsAidsToNavigation = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.BuoysBeaconsStructures = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ChartScaleBoundaries = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.DepthContours = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.DryingLine = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Lights = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.MagneticVariation = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.OtherMiscellaneous = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ProhibitedAndRestrictedAreas = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Seabed = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.ShipsRoutingSystemsAndFerryRoutes = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SpotSoundings = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.StandardMiscellaneous = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SubmarineCablesAndPipelines = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Tidal = _flag;

            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.BerthNumber = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.CurrentVelocity = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.GeographicNames = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.HeightOfIsletOrLandFeature = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.ImportantText = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.LightDescription = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.MagneticVariationAndSweptDepth = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NamesForPositionReporting = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NatureOfSeabed = _flag;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.NoteOnChartData = _flag;
            #endregion
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

        private async void LoadEnc_OnClick(object sender, RoutedEventArgs e)
        {
            // 指定海图文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Enc 031 file (*.031)|*.031|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            string selectedFilePath = openFileDialog.FileName;

            // 加载海图
            BaseMapView.Map.OperationalLayers.Clear();

            EncExchangeSet encExchangeSet = new EncExchangeSet(selectedFilePath);
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
            viewModel.IsEncLoaded = true;
            viewModel.IsEncDisplayed = true;
        }

        private void ConnectRadar_OnClick(object sender, RoutedEventArgs e)
        {
            // 指定雷达参数对话框
            var radarDialog = new RadarDialog();
            bool? dialogResult = radarDialog.ShowDialog();

            if (dialogResult == true)
            {
                var settings = (RadarSettingsViewModel)radarDialog.DataContext;

                // 获取雷达经纬度信息
                var viewModel = (RadarMonitorViewModel)DataContext;
                viewModel.IsRadarConnected = true;
                viewModel.RadarLongitude = double.Parse(settings.Longitude);
                viewModel.RadarLatitude = double.Parse(settings.Latitude);
                viewModel.RadarOrientation = settings.Orientation;

                viewModel.IsRingsDisplayed = true;
                DrawRings(viewModel.RadarLongitude, viewModel.RadarLatitude);

                // 获取雷达网络信息
                viewModel.RadarIpAddress =
                    $"{settings.IpPart1}.{settings.IpPart2}.{settings.IpPart3}.{settings.IpPart4}";
                viewModel.RadarPort = settings.Port;

                // 抓取 CAT240 网络包
                viewModel.CaptureCat240NetworkPackage();
                viewModel.IsEchoDisplayed = true;

                TransformRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale, 30);   // TODO: 如何在没有收到信号的时候确定maxdistance初始值
                TransformOpenGlRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale,
                    30);
            }
        }

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
                TransformOpenGlRadarEcho(viewModel.RadarLongitude, viewModel.RadarLatitude, viewModel.CurrentEncScale,
                    viewModel.MaxDistance);
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

            Brush scaleBrush = new SolidColorBrush(Colors.Black);
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
                FontSize = labelFontSize,
                Margin = new Thickness(0, canvasHeight / 2 + labelYOffset, 0, 0)
            };
            ScaleOverlay.Children.Add(zeroLabel);

            // 比例尺
            TextBlock scaleLabel = new TextBlock
            {
                Text = $"Scale 1:{(int)mapScale}",
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
                    FontSize = labelFontSize,
                    Margin = new Thickness(end * _dpcX - labelXOffset, canvasHeight / 2 + labelYOffset, 0, 0)
                };
                ScaleOverlay.Children.Add(TailLabel);
            }

            // KM尾标
            TextBlock kmLabel = new TextBlock
            {
                Text = "(KM)",
                FontSize = labelFontSize,
                Margin = new Thickness(scaleLength * _dpcX + labelXOffset, canvasHeight / 2 - labelYOffset, 0, 0)
            };
            ScaleOverlay.Children.Add(kmLabel);
        }

        private void DrawRings(double longitude, double latitude)
        {
            RingsOverlay.Children.Clear();

            Brush ringBrush = new SolidColorBrush(Colors.PaleGreen);   // 距离环的颜色
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
                    StrokeThickness = 2,
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
            OpenGlEchoOverlay.RadarMaxDistance = maxDistance;

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
    }
}
