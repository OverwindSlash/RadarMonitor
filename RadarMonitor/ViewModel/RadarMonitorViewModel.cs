using CAT240Parser;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using RadarMonitor.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RadarMonitor.ViewModel
{
    public delegate void EncChangedEventHandler(object sender, string encType, string encUri);
    public delegate void RadarChangedEventHandler(object sender, int radarId, RadarSetting radarSetting);
    public delegate void Cat240SpecChangedEventHandler(object sender, int radarId, Cat240Spec cat240Spec);
    public delegate void Cat240PackageReceivedEventHandler(object sender, int radarId, Cat240DataBlock data);
    public delegate void ViewPointChangedHandler(object sender, Viewpoint viewpoint);

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
        public event Cat240SpecChangedEventHandler OnCat240SpecChanged;
        public event Cat240PackageReceivedEventHandler OnCat240PackageReceived;
        public event ViewPointChangedHandler OnViewPointChanged;

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
        }

        public double Radar2StartAzimuth
        {
            get => _lastCat240DataItems[0] != null ? _lastCat240DataItems[1].StartAzimuthInDegree : 0.0;
        }

        public double Radar3StartAzimuth
        {
            get => _lastCat240DataItems[0] != null ? _lastCat240DataItems[2].StartAzimuthInDegree : 0.0;
        }

        public double Radar4StartAzimuth
        {
            get => _lastCat240DataItems[0] != null ? _lastCat240DataItems[3].StartAzimuthInDegree : 0.0;
        }

        public double Radar5StartAzimuth
        {
            get => _lastCat240DataItems[0] != null ? _lastCat240DataItems[4].StartAzimuthInDegree : 0.0;
        }
        #endregion


        // 雷达回波直角坐标系
        public const int CartesianSize = 2000;
        public const double HalfCartesianSize = CartesianSize / 2.0;

        private List<int[,]> _radarCartesianDatas = new()
        {
            new int[CartesianSize, CartesianSize], 
            new int[CartesianSize, CartesianSize], 
            new int[CartesianSize, CartesianSize], 
            new int[CartesianSize, CartesianSize], 
            new int[CartesianSize, CartesianSize]
        };

        private List<double> _radarRadiusIncrements = new()
            { 0, 0, 0, 0, 0 };

        private List<int> _radarScaledSteps = new ()
            { 1, 1, 1, 1, 1 };

        // 雷达数据及参数
        public List<int[,]> RadarCartesianDatas => _radarCartesianDatas;
        //public int[,] Radar1CartesianData => _radarCartesianDatas[0];
        //public int[,] Radar2CartesianData => _radarCartesianDatas[1];
        //public int[,] Radar3CartesianData => _radarCartesianDatas[2];
        //public int[,] Radar4CartesianData => _radarCartesianDatas[3];
        //public int[,] Radar5CartesianData => _radarCartesianDatas[4];


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
            client.OnCat240Received += OnReceivedCat240DataBlock;
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

                _radarCartesianDatas[radarId] = new int[CartesianSize, CartesianSize];
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

        public void OnReceivedCat240DataBlock(object sender, int radarId, Cat240DataBlock data)
        {
            var dataItems = data.Items;
            
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                if (_lastCat240DataItems[radarId] != null && 
                    _lastCat240DataItems[radarId].StartAzimuthInDegree == dataItems.StartAzimuthInDegree)
                {
                    // 避免切换雷达数据源时，因为没能及时处理数据而重发导致的问题
                    return;
                }

                // 首个数据包 或 数据包发生了变化
                if (dataItems.IsSpecChanged(_lastCat240DataItems[radarId]))
                {
                    switch (radarId)
                    {
                        case 0:
                            IsRadar1Connected = true; break;
                        case 1:
                            IsRadar2Connected = true; break;
                        case 2:
                            IsRadar3Connected = true; break;
                        case 3:
                            IsRadar4Connected = true; break;
                        case 4:
                            IsRadar5Connected = true; break;
                        default:
                            return;
                    }

                    OnCat240SpecChanged?.Invoke(this, radarId, new Cat240Spec(dataItems));

                    _radarRadiusIncrements[radarId] = HalfCartesianSize / dataItems.VideoBlocks.Count;
                    _radarScaledSteps[radarId] = dataItems.VideoBlocks.Count / CartesianSize;
                    RadarSettings[radarId].RadarMaxDistance = (int)(dataItems.CellDuration * dataItems.VideoCellDurationUnit * HalfC * dataItems.ValidCellsInDataBlock);
                }

                _lastCat240DataItems[radarId] = dataItems;

                PolarToCartesian(radarId, RadarSettings[radarId].RadarOrientation, dataItems);
                //OnCat240PackageReceived?.Invoke(this, data);   // 调整成只变更数据，不触发显示
            });
        }

        private void PolarToCartesian(int radarId, double radarOrientation, Cat240DataItems items)
        {
            double angleInRadians = (items.StartAzimuthInDegree + radarOrientation) * Math.PI / 180.0;

            var cosAzi = Math.Cos(angleInRadians);
            var sinAzi = Math.Sin(angleInRadians);

            double cosAziStep = _radarRadiusIncrements[radarId] * cosAzi;
            double sinAziStep = _radarRadiusIncrements[radarId] * sinAzi;

            for (int i = 0; i < items.VideoBlocks.Count; i += _radarScaledSteps[radarId])
            {
                int x = (int)(HalfCartesianSize + i * cosAziStep);
                int y = (int)(HalfCartesianSize + i * sinAziStep);

                if (x >= 0 && x < CartesianSize && y >= 0 && y < CartesianSize)
                {
                    int grayValue = (int)items.GetCellData(i);
                    _radarCartesianDatas[radarId][x, y] = grayValue;
                }
            }
        }
    }
}
