using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Hydrography;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.Win32;
using RadarMonitor.Model;
using RadarMonitor.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

        public MainWindow()
        {
            InitializeComponent();

            InitializeMapView();

            DataContext = new RadarMonitorViewModel();
        }

        private void InitializeMapView()
        {
            BaseMapView.Map = new Map();
            BaseMapView.IsAttributionTextVisible = false;

            // 海图显示配置：不显示 海床，深度点，地理名称
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.Seabed = false;
            EncEnvironmentSettings.Default.DisplaySettings.ViewingGroupSettings.SpotSoundings = false;
            EncEnvironmentSettings.Default.DisplaySettings.TextGroupVisibilitySettings.GeographicNames = false;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow);

            _dpiX = 96;
            _dpiY = 96;

            double adjustRatio = 1.1111;  // 为了让屏幕上实际显示的线段长度更准确而人为引入的调整参数

            _dpcX = _dpiX / 2.54 * adjustRatio;
            _dpcY = _dpiY / 2.54 * adjustRatio;
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
                var settingsViewModel = (RadarSettingsViewModel)radarDialog.DataContext;

                var viewModel = (RadarMonitorViewModel)DataContext;
                viewModel.IsRadarConnected = true;
                viewModel.RadarLongitude = double.Parse(settingsViewModel.Longitude);
                viewModel.RadarLatitude = double.Parse(settingsViewModel.Latitude);

                viewModel.IsRingsDisplayed = true;
                DrawRings(viewModel.RadarLongitude, viewModel.RadarLatitude);
            }
        }

        private void BaseMapView_OnViewpointChanged(object? sender, EventArgs e)
        {
            MapView mapView = (MapView)sender;
            var viewModel = (RadarMonitorViewModel)DataContext;
            viewModel.CurrentEncScale = mapView.MapScale;

            DrawScaleLine();
            DrawRings(viewModel.RadarLongitude, viewModel.RadarLatitude);
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

            CursorLongitude.Content = "Lon: " + location.X.ToString("F6");
            CursorLatitude.Content = "Lat:    " + location.Y.ToString("F6");
        }

        private void DisplayEcho_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox displayEchoCheckBox = (CheckBox)sender;

            if (displayEchoCheckBox.IsChecked.Value)
            {
                EchoOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                EchoOverlay.Visibility = Visibility.Hidden;
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
            }
            else
            {
                BaseMapView.Visibility = Visibility.Hidden;
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
                    newCenterPoint = new MapPoint(viewModel.RadarLongitude, viewModel.RadarLatitude, SpatialReferences.Wgs84);
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

        private void ZoomView(bool isZoomIn, double step = 1.5)
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

            Brush ringBrush = new SolidColorBrush(Colors.Chartreuse);   // 距离环的颜色

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
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(circle, radarX - radius);
                Canvas.SetTop(circle, radarY - radius);
                RingsOverlay.Children.Add(circle);

                // 添加距离文字
                double radiusInKm = ringOffset * (BaseMapView.MapScale / 100000.0);
                ringOffset += ringStep;

                TextBlock distanceText = new TextBlock();
                distanceText.Text = radiusInKm.ToString("F1") + "KM";
                distanceText.Foreground = ringBrush;
                distanceText.FontSize = 14;

                Canvas.SetLeft(distanceText, radarX + radius + 2);
                Canvas.SetTop(distanceText, radarY);
                RingsOverlay.Children.Add(distanceText);
            }
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
