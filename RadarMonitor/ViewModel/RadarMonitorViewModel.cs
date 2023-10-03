using RadarMonitor.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using CAT240Parser;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RadarMonitor.ViewModel
{
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
        }
    }
}
