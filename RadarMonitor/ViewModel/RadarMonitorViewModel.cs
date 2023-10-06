using CAT240Parser;
using OpenCvSharp;
using RadarMonitor.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RadarMonitor.ViewModel
{
    public delegate void PolarLineUpdatedEventHandler(object sender, List<Tuple<int, int, int>> updatedPixels);
    public delegate void ImageUpdatedEventHandler(object sender, Mat image);

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

        private bool _isEncLoaded;
        private bool _isRadarConnected;

        private double _currentEncScale;

        private bool _isEncDisplayed;
        private bool _isRingsDisplayed;
        private bool _isEchoDisplayed;

        private List<PresetLocation> _presetLocations = new List<PresetLocation>();

        private double _radarLongitude;
        private double _radarLatitude;

        private string _radarIpAddress;
        private int _radarPort;

        private MulticastClient _client;

        private double _startAzimuth;
        private double _endAzimuth;
        private int _startRange;
        private int _cellDuration;
        private bool _cellCompression;
        private int _cellResolution;
        private int _cellCount;
        private int _videoBlockCount;
        
        public const int CartesianSzie = 2000;
        private int[,] _cartesianData = new int[CartesianSzie, CartesianSzie];

        public event PolarLineUpdatedEventHandler OnPolarLineUpdated;
        public event ImageUpdatedEventHandler OnImageUpdated;

        //private byte[] _echoData = new byte[CartesianSzie * CartesianSzie * 4];

        #region Properties
        public bool IsEncLoaded
        {
            get => _isEncLoaded;
            set
            {
                SetField(ref _isEncLoaded, value, "IsEncLoaded");
            }
        }

        public bool IsRadarConnected
        {
            get => _isRadarConnected;
            set
            {
                SetField(ref _isRadarConnected, value, "IsRadarConnected");
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

        public double RadarLongitude
        {
            get => _radarLongitude;
            set
            {
                SetField(ref _radarLongitude, value, "RadarLongitude");
            }
        }

        public double RadarLatitude
        {
            get => _radarLatitude;
            set
            {
                SetField(ref _radarLatitude, value, "RadarLatitude");
            }
        }

        public string RadarIpAddress
        {
            get => _radarIpAddress;
            set
            {
                SetField(ref _radarIpAddress, value, "RadarIpAddress");
            }
        }

        public int RadarPort
        {
            get => _radarPort;
            set
            {
                SetField(ref _radarPort, value, "RadarPort");
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
        #endregion


        public RadarMonitorViewModel()
        {
            _radarIpAddress = string.Empty;
            try
            {
                LoadPresetLocations();
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

        public void CaptureCat240NetworkPackage()
        {
            StopCaptureCat240NetworkPackage();

            _client = new MulticastClient(_radarIpAddress, RadarPort);
            _client.SetupMulticast(true);
            _client.Multicast = "239.255.0.1";
            _client.OnCat240Received += OnReceivedCat240DataBlock;
            _client.Connect();
        }

        public void StopCaptureCat240NetworkPackage()
        {
            if (_client != null)
            {
                _client.DisconnectAndStop();
            }
        }

        public void OnReceivedCat240DataBlock(object sender, Cat240DataBlock data)
        {
            StartAzimuth = data.Items.StartAzimuthInDegree;
            EndAzimuth = data.Items.EndAzimuthInDegree;
            StartRange = (int)data.Items.StartRange;
            CellCompression = data.Items.IsDataCompressed;
            CellDuration = (int)data.Items.CellDuration;
            CellResolution = data.Items.VideoResolution;
            CellCount = (int)data.Items.ValidCellsInDataBlock;
            VideoBlockCount = (int)data.Items.ValidCellsInDataBlock;
            //ExampleScene.OnReceivedCat240DataBlock(sender, data);

            var updatedPixels = PolarToCartesian(data);
            OnPolarLineUpdated?.Invoke(this, updatedPixels);
        }

        private List<Tuple<int, int, int>> PolarToCartesian(Cat240DataBlock data)
        {
            Cat240DataItems items = data.Items;

            double angleInRadians = (items.StartAzimuthInDegree + 60.0) * Math.PI / 180.0;
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

        private static void SaveImage(int[,] cartesian, string filename)
        {
            // 读取灰度图像数据，假设grayData为您的二维数组
            int width = cartesian.GetLength(0); // 图像宽度
            int height = cartesian.GetLength(1); // 图像高度

            // 创建一个新的RGB图像
            Mat rgbImage = new Mat(height, width, MatType.CV_8UC4);

            // 定义颜色，例如蓝色，以及透明度
            Scalar color = new Scalar(0, 255, 0); // BGR颜色值，这里为纯蓝色
            byte alpha = 0; // 初始透明度为0

            // 迭代图像的每个像素
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte grayValue = (byte)cartesian[x, y];

                    // 将灰度值映射到透明度范围
                    alpha = (byte)grayValue; // 较小的灰度值将产生更高的透明度

                    // 设置像素颜色和透明度
                    Vec4b pixel = new Vec4b((byte)color.Val0, (byte)color.Val1, (byte)color.Val2, alpha);
                    rgbImage.Set<Vec4b>(y, x, pixel);
                }
            }

            // 保存处理后的图像
            Cv2.ImWrite(filename, rgbImage);
        }
    }
}
