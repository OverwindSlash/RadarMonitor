using CAT240Parser;
using RadarMonitor.Model;
using Silk.WPF.OpenGL.Scene;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RadarMonitor.ViewModel
{
    public delegate void EncChangedEventHandler(object sender, string encUri);
    public delegate void MainRadarChangedEventHandler(object sender, RadarSettings radarSettings);
    public delegate void Cat240SpecChangedEventHandler(object sender, Cat240Spec cat240Spec);
    public delegate void Cat240PackageReceivedEventHandler(object sender, Cat240DataBlock data, List<Tuple<int, int, int>> updatedPixels);
    public delegate void ViewPointChangedHandler(object sender, ViewPoint viewPoint);

    public class RadarMonitorViewModel : INotifyPropertyChanged
    {
        #region Notify Property
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        // 海图相关属性
        private bool _isEncLoaded;
        private string _encUri;
        private double _currentEncScale;
        private bool _isEncDisplayed;

        private bool _isPresetLocationLoaded;
        private List<PresetLocation> _presetLocations = new List<PresetLocation>();

        // 雷达静态属性
        private bool _isRadarConnected;
        private RadarSettings _radarSettings;
        private bool _isRingsDisplayed;
        private bool _isEchoDisplayed;
        private bool _isOpenGlEchoDisplayed;

        // 雷达动态属性
        private Cat240DataItems _lastCat240DataItems = null;
        private double _lastStartAzimuth = 0;
        private double _startAzimuth;
        private double _endAzimuth;
        private int _startRange;
        private int _cellDuration;
        private bool _cellCompression;
        private int _cellResolution;
        private int _cellCount;
        private int _videoBlockCount;
        private int _maxDistance;
        
        // 雷达回波直角坐标系
        public const int CartesianSzie = 2000;
        private int[,] _cartesianData = new int[CartesianSzie, CartesianSzie];

        // 事件
        public event EncChangedEventHandler OnEncChanged;
        public event MainRadarChangedEventHandler OnMainRadarChanged;
        public event Cat240SpecChangedEventHandler OnCat240SpecChanged;
        public event Cat240PackageReceivedEventHandler OnCat240PackageReceived;
        public event ViewPointChangedHandler OnViewPointChanged;

        private MulticastClient _client;

        #region Properties
        public bool IsEncLoaded
        {
            get => _isEncLoaded;
            set
            {
                SetField(ref _isEncLoaded, value, "IsEncLoaded");
                SetField(ref _isPresetLocationLoaded, value && _presetLocations.Count > 0, "IsPresetLocationsLoaded");
            }
        }

        public double CurrentEncScale
        {
            get => _currentEncScale;
            set
            {
                SetField(ref _currentEncScale, value, "CurrentEncScale");
            }
        }

        public bool IsEncDisplayed
        {
            get => _isEncDisplayed;
            set
            {
                SetField(ref _isEncDisplayed, value, "IsEncDisplayed");
            }
        }

        public bool IsPresetLocationsLoaded => _isPresetLocationLoaded;

        public bool IsRadarConnected
        {
            get => _isRadarConnected;
            set
            {
                SetField(ref _isRadarConnected, value, "IsRadarConnected");
            }
        }

        public RadarSettings MainRadarSettings
        {
            get => _radarSettings;
            set
            {
                if (_radarSettings != value)
                {
                    SetField(ref _radarSettings, value, "MainRadarSettings");
                    OnMainRadarChanged?.Invoke(this, _radarSettings);
                }
            }
        }
        public double RadarLongitude => _radarSettings.Longitude;
        public double RadarLatitude => _radarSettings.Latitude;
        public double RadarOrientation => _radarSettings.Orientation;
        public string RadarIpAddress => _radarSettings.Ip;
        public int RadarPort => _radarSettings.Port;

        public bool IsRingsDisplayed
        {
            get => _isRingsDisplayed;
            set
            {
                SetField(ref _isRingsDisplayed, value, "IsRingsDisplayed");
            }
        }

        public bool IsEchoDisplayed
        {
            get => _isEchoDisplayed;
            set
            {
                SetField(ref _isEchoDisplayed, value, "IsEchoDisplayed");
            }
        }

        public bool IsOpenGlEchoDisplayed
        {
            get => _isOpenGlEchoDisplayed;
            set
            {
                SetField(ref _isOpenGlEchoDisplayed, value, "IsOpenGlEchoDisplayed");
            }
        }

        public double StartAzimuth
        {
            get => _startAzimuth;
            set
            {
                SetField(ref _startAzimuth, value, "StartAzimuth");
            }
        }

        public double EndAzimuth
        {
            get => _endAzimuth;
            set
            {
                SetField(ref _endAzimuth, value, "EndAzimuth");
            }
        }

        public int StartRange
        {
            get => _startRange;
            set
            {
                SetField(ref _startRange, value, "StartRange");
            }
        }

        public int CellDuration
        {
            get => _cellDuration;
            set
            {
                SetField(ref _cellDuration, value, "CellDuration");
            }
        }

        public bool CellCompression
        {
            get => _cellCompression;
            set
            {
                SetField(ref _cellCompression, value, "CellCompression");
            }
        }

        public int CellResolution
        {
            get => _cellResolution;
            set
            {
                SetField(ref _cellResolution, value, "CellResolution");
            }
        }

        public int CellCount
        {
            get => _cellCount;
            set
            {
                SetField(ref _cellCount, value, "CellCount");
            }
        }

        public int VideoBlockCount
        {
            get => _videoBlockCount;
            set
            {
                SetField(ref _videoBlockCount, value, "VideoBlockCount");
            }
        }

        public int MaxDistance
        {
            get => _maxDistance;
            set
            {
                SetField(ref _maxDistance, value, "MaxDistance");
            }
        }
        #endregion

        public RadarMonitorViewModel()
        {
            try
            {
                LoadPresetLocations();
                _radarSettings = new RadarSettings();
            }
            catch (Exception e)
            {
                // TODO: Show exception message.
            }
        }

        private void LoadPresetLocations()
        {
            string contents = File.ReadAllText("Presets/preset-locations.yaml");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            _presetLocations = deserializer.Deserialize<List<PresetLocation>>(contents);
        }

        public PresetLocation GetPresetLocation(int index)
        {
            if (index > _presetLocations.Count - 1)
            {
                return new PresetLocation();
            }

            return _presetLocations[index];
        }

        public void RecordNewEncUri(string encUri)
        {
            if (_encUri != encUri)
            {
                _encUri = encUri;
                IsEncLoaded = true;
                IsEncDisplayed = true;
                OnEncChanged?.Invoke(this, _encUri);
            }
        }

        public void CaptureCat240NetworkPackage()
        {
            StopCaptureCat240NetworkPackage();

            _client = new MulticastClient(RadarIpAddress, RadarPort);
            _client.SetupMulticast(true);
            _client.Multicast = "239.255.0.1";
            _client.OnCat240Received += OnReceivedCat240DataBlock;
            _client.Connect();
        }

        public void StopCaptureCat240NetworkPackage()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client.Dispose();

                _cartesianData = new int[CartesianSzie, CartesianSzie];
            }
        }

        public void DisposeCat240Parser()
        {
            if (_client != null)
            {
                _client.DisconnectAndStop();
            }
        }

        public void OnReceivedCat240DataBlock(object sender, Cat240DataBlock data)
        {
            var dataItems = data.Items;

            // 避免切换雷达数据源时，因为没能及时处理数据而重发导致的问题
            if (_lastStartAzimuth == dataItems.StartAzimuthInDegree)
            {
                return;
            }

            // 同一雷达每个数据包都会变的信息
            StartAzimuth = dataItems.StartAzimuthInDegree;
            EndAzimuth = dataItems.EndAzimuthInDegree;
            StartRange = (int)dataItems.StartRange;

            CellCompression = dataItems.IsDataCompressed;
            CellDuration = (int)dataItems.CellDuration;
            CellResolution = dataItems.VideoResolution;
            CellCount = (int)dataItems.ValidCellsInDataBlock;
            VideoBlockCount = (int)dataItems.ValidCellsInDataBlock;
            MaxDistance = (int)(dataItems.CellDuration * dataItems.VideoCellDurationUnit * 300000 / 2 * dataItems.ValidCellsInDataBlock);

            // TODO: 疑问点
            // 同一雷达每个数据包基本不变的信息
            //if (dataItems.IsSpecChanged(_lastCat240DataItems))
            {
                OnCat240SpecChanged?.Invoke(this, new Cat240Spec(dataItems));
            }

            _lastCat240DataItems = dataItems;
            _lastStartAzimuth = StartAzimuth;

            var updatedPixels = PolarToCartesian(data);
            OnCat240PackageReceived?.Invoke(this, data, updatedPixels);
        }

        private List<Tuple<int, int, int>> PolarToCartesian(Cat240DataBlock data)
        {
            Cat240DataItems items = data.Items;

            double angleInRadians = (items.StartAzimuthInDegree + RadarOrientation) * Math.PI / 180.0;
            var cosAzi = Math.Cos(angleInRadians);
            var sinAzi = Math.Sin(angleInRadians);

            double radiusIncrement = CartesianSzie / 2.0 / items.VideoBlocks.Count;

            double cosAziStep = radiusIncrement * cosAzi;
            double sinAziStep = radiusIncrement * sinAzi;

            double halfSize = CartesianSzie / 2.0;

            int stride = RadarMonitorViewModel.CartesianSzie * 4;
            List<Tuple<int, int, int>> updatedPixels = new List<Tuple<int, int, int>>();

            int index = 0;
            for (int i = 0; i < items.VideoBlocks.Count; i++)
            {
                int x = (int)(halfSize + i * cosAziStep);
                int y = (int)(halfSize + i * sinAziStep);

                if (x >= 0 && x < CartesianSzie && y >= 0 && y < CartesianSzie)
                {
                    int grayValue = (int)items.GetCellData(i);
                    _cartesianData[x, y] = grayValue;
                    updatedPixels.Add(new Tuple<int, int, int>(x, y, grayValue));
                }
            }

            return updatedPixels;
        }
    }
}
