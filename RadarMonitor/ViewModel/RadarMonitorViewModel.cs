using CAT240Parser;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using RadarMonitor.Model;
using Silk.WPF.OpenGL.Scene;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RadarMonitor.ViewModel
{
    public delegate void EncChangedEventHandler(object sender, string encType, string encUri);
    public delegate void RadarChangedEventHandler(object sender, int radarId, RadarSetting radarSetting);
    public delegate void RadarConnectionStatusChangedHandler(object sender, int radarId, string ip, int port, RadarConnectionStatus status);
    public delegate void Cat240SpecChangedEventHandler(object sender, int radarId, Cat240Spec cat240Spec);
    //public delegate void Cat240PackageReceivedEventHandler(object sender, int radarId, Cat240DataBlock data);
    public delegate void Cat240PackageReceivedOpenGLEventHandler(object sender, RadarDataReceivedEventArgs e);

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

        // 事件
        public event EncChangedEventHandler OnEncChanged;
        public event RadarChangedEventHandler OnRadarChanged;
        public event RadarConnectionStatusChangedHandler OnRadarConnectionStatusChanged;
        public event Cat240SpecChangedEventHandler OnCat240SpecChanged;
        //public event Cat240PackageReceivedEventHandler OnCat240PackageReceived;
        public event Cat240PackageReceivedOpenGLEventHandler OnCat240PackageReceivedOpenGLEvent;

        // 用户配置信息
        private UserConfiguration _userConfiguration;
        private bool _isPresetLocationLoaded;
        
        #region UserConfiguration Properties
        public UserConfiguration Configuration
        {
            get => _userConfiguration;
            set
            {
                SetField(ref _userConfiguration, value, "UserConfiguration");
            }
        }

        public bool IsPresetLocationsLoaded
        {
            get => _isPresetLocationLoaded;
            set
            {
                SetField(ref _isPresetLocationLoaded, value, "IsPresetLocationsLoaded");
            }
        }
        #endregion


        // OpenGL 回波显示
        private bool _isOpenGlEchoDisplayed;
        public bool IsOpenGlEchoDisplayed
        {
            get => _isOpenGlEchoDisplayed;
            set
            {
                SetField(ref _isOpenGlEchoDisplayed, value, "IsOpenGlEchoDisplayed");
            }
        }

        // 海图相关属性
        private bool _isEncLoaded;
        private bool _isEncDisplayed;
        private string _encType;
        private string _encUri;
        private double _encLongitude;
        private double _encLatitude;
        private double _encScale;

        #region Enc Properties
        public bool IsEncLoaded
        {
            get => _isEncLoaded;
            set
            {
                SetField(ref _isEncLoaded, value, "IsEncLoaded");
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

        public string EncType
        {
            get => _encType;
            set
            {
                SetField(ref _encType, value, "EncType");
            }
        }

        public string EncUri
        {
            get => _encUri;
            set
            {
                SetField(ref _encUri, value, "EncUri");
            }
        }

        public double EncLongitude
        {
            get => _encLongitude;
            set
            {
                SetField(ref _encLongitude, value, "EncLongitude");
            }

        }

        public double EncLatitude
        {
            get => _encLatitude;
            set
            {
                SetField(ref _encLatitude, value, "EncLatitude");
            }
        }

        public double EncScale
        {
            get => _encScale;
            set
            {
                SetField(ref _encScale, value, "EncScale");
            }
        }
        #endregion


        // 雷达静态属性
        private const int HalfC = 300000 / 2;
        private List<RadarSetting> _radarSettings = new()
        {
            new RadarSetting(),
            new RadarSetting(),
            new RadarSetting(),
            new RadarSetting(),
            new RadarSetting()
        };

        #region Radar Properties
        public bool IsRadarConnected
        {
            get => IsRadar1Connected || 
                   IsRadar2Connected || 
                   IsRadar3Connected || 
                   IsRadar4Connected ||
                   IsRadar5Connected;
        }

        public List<RadarSetting> RadarSettings
        {
            get => _radarSettings;
            set
            {
                SetField(ref _radarSettings, value, "RadarSetting");
                int radarId = 0;
                foreach (var radarSetting in _radarSettings)
                {
                    OnRadarChanged?.Invoke(this, radarId++, radarSetting);
                }
            }
        }

        // Radar 1
        public bool IsRadar1Connected
        {
            get => _radarSettings[0].IsConnected;
            set
            {
                _radarSettings[0].IsConnected = value;
                OnPropertyChanged("IsRadar1Connected");
                OnPropertyChanged("IsRadarConnected");
            }
        }

        public bool IsRadar1RingsDisplayed
        {
            get => _radarSettings[0].IsRingsDisplayed;
            set
            {
                _radarSettings[0].IsRingsDisplayed = value;
                OnPropertyChanged("IsRadar1RingsDisplayed");
            }
        }

        public bool IsRadar1EchoDisplayed
        {
            get => _radarSettings[0].IsEchoDisplayed;
            set
            {
                _radarSettings[0].IsEchoDisplayed = value;
                OnPropertyChanged("IsRadar1EchoDisplayed");
            }
        }

        public bool IsRadar1OpenGlEchoDisplayed
        {
            get => _radarSettings[0].IsOpenGlEchoDisplayed;
            set
            {
                _radarSettings[0].IsOpenGlEchoDisplayed = value;
                OnPropertyChanged("IsRadar1OpenGlEchoDisplayed");
            }
        }

        // Radar 2
        public bool IsRadar2Connected
        {
            get => _radarSettings[1].IsConnected;
            set
            {
                _radarSettings[1].IsConnected = value;
                OnPropertyChanged("IsRadar2Connected");
                OnPropertyChanged("IsRadarConnected");
            }
        }

        public bool IsRadar2RingsDisplayed
        {
            get => _radarSettings[1].IsRingsDisplayed;
            set
            {
                _radarSettings[1].IsRingsDisplayed = value;
                OnPropertyChanged("IsRadar2RingsDisplayed");
            }
        }

        public bool IsRadar2EchoDisplayed
        {
            get => _radarSettings[1].IsEchoDisplayed;
            set
            {
                _radarSettings[1].IsEchoDisplayed = value;
                OnPropertyChanged("IsRadar2EchoDisplayed");
            }
        }

        public bool IsRadar2OpenGlEchoDisplayed
        {
            get => _radarSettings[1].IsOpenGlEchoDisplayed;
            set
            {
                _radarSettings[1].IsOpenGlEchoDisplayed = value;
                OnPropertyChanged("IsRadar2OpenGlEchoDisplayed");
            }
        }

        // Radar 3
        public bool IsRadar3Connected
        {
            get => _radarSettings[2].IsConnected;
            set
            {
                _radarSettings[2].IsConnected = value;
                OnPropertyChanged("IsRadar3Connected");
                OnPropertyChanged("IsRadarConnected");
            }
        }

        public bool IsRadar3RingsDisplayed
        {
            get => _radarSettings[2].IsRingsDisplayed;
            set
            {
                _radarSettings[2].IsRingsDisplayed = value;
                OnPropertyChanged("IsRadar3RingsDisplayed");
            }
        }

        public bool IsRadar3EchoDisplayed
        {
            get => _radarSettings[2].IsEchoDisplayed;
            set
            {
                _radarSettings[2].IsEchoDisplayed = value;
                OnPropertyChanged("IsRadar3EchoDisplayed");
            }
        }

        public bool IsRadar3OpenGlEchoDisplayed
        {
            get => _radarSettings[2].IsOpenGlEchoDisplayed;
            set
            {
                _radarSettings[2].IsOpenGlEchoDisplayed = value;
                OnPropertyChanged("IsRadar3OpenGlEchoDisplayed");
            }
        }

        // Radar 4
        public bool IsRadar4Connected
        {
            get => _radarSettings[3].IsConnected;
            set
            {
                _radarSettings[3].IsConnected = value;
                OnPropertyChanged("IsRadar4Connected");
                OnPropertyChanged("IsRadarConnected");
            }
        }

        public bool IsRadar4RingsDisplayed
        {
            get => _radarSettings[3].IsRingsDisplayed;
            set
            {
                _radarSettings[3].IsRingsDisplayed = value;
                OnPropertyChanged("IsRadar4RingsDisplayed");
            }
        }

        public bool IsRadar4EchoDisplayed
        {
            get => _radarSettings[3].IsEchoDisplayed;
            set
            {
                _radarSettings[3].IsEchoDisplayed = value;
                OnPropertyChanged("IsRadar4EchoDisplayed");
            }
        }

        public bool IsRadar4OpenGlEchoDisplayed
        {
            get => _radarSettings[3].IsOpenGlEchoDisplayed;
            set
            {
                _radarSettings[3].IsOpenGlEchoDisplayed = value;
                OnPropertyChanged("IsRadar4OpenGlEchoDisplayed");
            }
        }

        // Radar 5
        public bool IsRadar5Connected
        {
            get => _radarSettings[4].IsConnected;
            set
            {
                _radarSettings[4].IsConnected = value;
                OnPropertyChanged("IsRadar5Connected");
                OnPropertyChanged("IsRadarConnected");
            }
        }

        public bool IsRadar5RingsDisplayed
        {
            get => _radarSettings[4].IsRingsDisplayed;
            set
            {
                _radarSettings[4].IsRingsDisplayed = value;
                OnPropertyChanged("IsRadar5RingsDisplayed");
            }
        }

        public bool IsRadar5EchoDisplayed
        {
            get => _radarSettings[4].IsEchoDisplayed;
            set
            {
                _radarSettings[4].IsEchoDisplayed = value;
                OnPropertyChanged("IsRadar5EchoDisplayed");
            }
        }

        public bool IsRadar5OpenGlEchoDisplayed
        {
            get => _radarSettings[4].IsOpenGlEchoDisplayed;
            set
            {
                _radarSettings[4].IsOpenGlEchoDisplayed = value;
                OnPropertyChanged("IsRadar5OpenGlEchoDisplayed");
            }
        }
        #endregion


        // 雷达动态属性
        private List<Cat240DataItems> _lastCat240DataItems = new()
            { null, null, null, null, null};

        #region Radar Status
        
        public double Radar1StartAzimuth
        {
            get => _lastCat240DataItems[0] != null ? _lastCat240DataItems[0].StartAzimuthInDegree : 0.0;
            set
            {
                _lastCat240DataItems[0].StartAzimuthInDegree = value;
                OnPropertyChanged("Radar1StartAzimuth");
            }
        }

        public double Radar2StartAzimuth
        {
            get => _lastCat240DataItems[1] != null ? _lastCat240DataItems[1].StartAzimuthInDegree : 0.0;
            set
            {
                _lastCat240DataItems[1].StartAzimuthInDegree = value;
                OnPropertyChanged("Radar2StartAzimuth");
            }
        }

        public double Radar3StartAzimuth
        {
            get => _lastCat240DataItems[2] != null ? _lastCat240DataItems[2].StartAzimuthInDegree : 0.0;
            set
            {
                _lastCat240DataItems[2].StartAzimuthInDegree = value;
                OnPropertyChanged("Radar3StartAzimuth");
            }
        }

        public double Radar4StartAzimuth
        {
            get => _lastCat240DataItems[3] != null ? _lastCat240DataItems[3].StartAzimuthInDegree : 0.0;
            set
            {
                _lastCat240DataItems[3].StartAzimuthInDegree = value;
                OnPropertyChanged("Radar4StartAzimuth");
            }
        }

        public double Radar5StartAzimuth
        {
            get => _lastCat240DataItems[4] != null ? _lastCat240DataItems[4].StartAzimuthInDegree : 0.0;
            set
            {
                _lastCat240DataItems[4].StartAzimuthInDegree = value;
                OnPropertyChanged("Radar5StartAzimuth");
            }
        }
        #endregion

        // UDP Clients
        private List<MulticastClient> _udpClients = new()
        {
            null, null, null, null, null
        };

        public RadarMonitorViewModel()
        {
            Configuration = UserConfiguration.LoadConfiguration("Config/default.yaml");

            RadarSettings = Configuration.RadarSettings;

            // TODO: 后续可以改善做到每个预置位按钮单独控制
            IsPresetLocationsLoaded = RadarSettings.Count > 0;
        }

        public void RecordEncTypeAndUri(string encType, string encUri)
        {
            EncType = encType;
            EncUri = encUri;
            IsEncLoaded = true;
            IsEncDisplayed = true;
            OnEncChanged?.Invoke(this, _encType, _encUri);
        }

        public void RecordEncViewPoint(Viewpoint newViewpoint)
        {
            var mapPoint = newViewpoint.TargetGeometry as MapPoint;

            EncLongitude = mapPoint.X;
            EncLatitude = mapPoint.Y;
            EncScale = newViewpoint.TargetScale;
        }

        public void CaptureCat240NetworkPackage(int radarId, string radarIp, int radarPort)
        {
            if (radarId > _udpClients.Count - 1)
            {
                return;
            }

            StopCaptureCat240NetworkPackage(radarId);

            var client = new MulticastClient(radarId, radarIp, radarPort);
            client.SetupMulticast(true);
            client.Multicast = $"239.255.0.{radarId}";
            client.OnUdpConnected += OnUdpConnected;
            client.OnCat240Received += OnReceivedCat240DataBlock;
            client.OnUdpDisconnected += OnUdpDisconnected;
            client.OnUdpError += OnUdpError;
            client.Connect();

            _udpClients.Add(client);
        }

        public void StopCaptureCat240NetworkPackage(int radarId)
        {
            if (radarId > _udpClients.Count - 1)
            {
                return;
            }

            var client = _udpClients[radarId];
            if (client != null)
            {
                client.Disconnect();
                client.Dispose();
            }
        }

        public void DisposeCat240Parser()
        {
            foreach (var client in _udpClients)
            {
                if (client != null)
                {
                    client.DisconnectAndStop();
                }
            }
        }

        private void OnUdpConnected(object sender, int clientId, string ip, int port)
        {
            // 这里只是 UDP 端口连接，不是雷达连接，所以不用改变雷达状态
            OnRadarConnectionStatusChanged?.Invoke(this, clientId, ip, port, RadarConnectionStatus.Connected);
        }

        private void OnUdpDisconnected(object sender, int clientId, string ip, int port)
        {
            SetRadarConnectionStatus(clientId, false);
            OnRadarConnectionStatusChanged?.Invoke(this, clientId, ip, port, RadarConnectionStatus.Disconnected);
        }

        private void OnUdpError(object sender, int clientId, string ip, int port)
        {
            SetRadarConnectionStatus(clientId, false);
            OnRadarConnectionStatusChanged?.Invoke(this, clientId, ip, port, RadarConnectionStatus.Error);
        }

        public void OnReceivedCat240DataBlock(object sender, int radarId, Cat240DataBlock data)
        {
            if (radarId > RadarSettings.Count - 1)
            {
                return;
            }

            var dataItems = data.Items;
            
            if (!RadarSettings[radarId].IsRadarEnabled)
            {
                // 雷达若没启用，则略过数据包
                return;
            }

            // 雷达连接后改变连接状态
            SetRadarConnectionStatus(radarId, true);

            // 同一雷达每个数据包基本不变的信息
            if (dataItems.IsSpecChanged(_lastCat240DataItems[radarId]))
            {
                RadarSettings[radarId].RadarMaxDistance =
                    (int)Math.Ceiling(dataItems.CellDuration * dataItems.VideoCellDurationUnit * HalfC * dataItems.ValidCellsInDataBlock);

                OnCat240SpecChanged?.Invoke(this, radarId, new Cat240Spec(dataItems));
                OnRadarConnectionStatusChanged?.Invoke(this, radarId, string.Empty, 0, RadarConnectionStatus.Normal);
                _lastCat240DataItems[radarId] = dataItems;
            }

            SetRadarAzimuth(radarId, dataItems.StartAzimuthInDegree);

            List<float> DataArr = new List<float>((int)dataItems.VideoBlocks.Count + 1);
            var t = DateTime.Now;
            var ts = t.Hour * 60 * 60 * 1000 + t.Minute * 60 * 1000 + t.Second * 1000 + t.Millisecond;
            DataArr.Add(ts);

            for (int i = 0; i < data.Items.ValidCellsInDataBlock; i++)
            {
                var color = (float)data.Items.GetCellData(i) / 255;
                DataArr.Add(color);
            }

            OnCat240PackageReceivedOpenGLEvent?.Invoke(this, new RadarDataReceivedEventArgs(radarId, data.Items.StartAzimuth, DataArr));
        }

        private void SetRadarConnectionStatus(int radarId, bool status)
        {
            switch (radarId)
            {
                case 0:
                    IsRadar1Connected = status;
                    break;
                case 1:
                    IsRadar2Connected = status;
                    break;
                case 2:
                    IsRadar3Connected = status;
                    break;
                case 3:
                    IsRadar4Connected = status;
                    break;
                case 4:
                    IsRadar5Connected = status;
                    break;
            }
        }

        private void SetRadarAzimuth(int radarId, double azimuthInDegree)
        {
            switch (radarId)
            {
                case 0:
                    Radar1StartAzimuth = azimuthInDegree;
                    break;
                case 1:
                    Radar2StartAzimuth = azimuthInDegree;
                    break;
                case 2:
                    Radar3StartAzimuth = azimuthInDegree;
                    break;
                case 3:
                    Radar4StartAzimuth = azimuthInDegree;
                    break;
                case 4:
                    Radar5StartAzimuth = azimuthInDegree;
                    break;
            }
        }

        #region Utilities
        private static double CalculateDistance(double lon1, double lat1, double lon2, double lat2)
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

        private static double CalculateAzimuth(double lon1, double lat1, double lon2, double lat2)
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
        #endregion
    }
}
