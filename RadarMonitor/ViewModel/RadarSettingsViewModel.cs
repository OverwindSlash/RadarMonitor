using RadarMonitor.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RadarMonitor.ViewModel
{
    public class RadarSettingsViewModel : INotifyPropertyChanged
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

        private string _longitude;
        private string _latitude;
        private double _orientation;
        private int _ipPart1;
        private int _ipPart2;
        private int _ipPart3;
        private int _ipPart4;
        private int _port;

        private bool _validated;

        public string Longitude
        {
            get { return _longitude; }
            set
            {
                if (IsLongitudeValid(value))
                {
                    SetField(ref _longitude, value, "Longitude");
                }
            }
        }

        public string Latitude
        {
            get { return _latitude; }
            set
            {
                if (IsLatitudeValid(value))
                {
                    SetField(ref _latitude, value, "Latitude");
                }
            }
        }

        public double Orientation
        {
            get => _orientation;
            set
            {
                if (IsValidOrientation(value))
                {
                    SetField(ref _orientation, value, "Orientation");
                }
            }
        }

        public int IpPart1
        {
            get { return _ipPart1; }
            set
            {
                if (IsValidIpAddressPart(value))
                {
                    SetField(ref _ipPart1, value, "IpPart1");
                }
            }
        }

        public int IpPart2
        {
            get { return _ipPart2; }
            set
            {
                if (IsValidIpAddressPart(value))
                {
                    SetField(ref _ipPart2, value, "IpPart2");
                }
            }
        }

        public int IpPart3
        {
            get { return _ipPart3; }
            set
            {
                if (IsValidIpAddressPart(value))
                {
                    SetField(ref _ipPart3, value, "IpPart3");
                }
            }
        }

        public int IpPart4
        {
            get { return _ipPart4; }
            set
            {
                if (IsValidIpAddressPart(value))
                {
                    SetField(ref _ipPart4, value, "IpPart4");
                }
            }
        }

        public int Port
        {
            get { return _port; }
            set
            {
                if (IsValidPort(value))
                {
                    SetField(ref _port, value, "Port");
                }
            }
        }

        public int CurrentId { get; set; }

        public RadarSettingsViewModel()
        {
            _longitude = "118.82300101666256";
            _latitude = "32.03646416724121";
            _orientation = 0;

            _ipPart1 = 127;
            _ipPart2 = 0;
            _ipPart3 = 0;
            _ipPart4 = 1;
            _port = 30101;
        }

        public RadarSettingsViewModel(RadarSetting current)
        {
            Longitude = current.Longitude.ToString();
            Latitude = current.Latitude.ToString();
            Orientation = current.Orientation;

            IpPart1 = int.Parse(current.Ip.Split('.')[0]);
            IpPart2 = int.Parse(current.Ip.Split('.')[1]);
            IpPart3 = int.Parse(current.Ip.Split('.')[2]);
            IpPart4 = int.Parse(current.Ip.Split('.')[3]);

            Port = current.Port;
            CurrentId = current.Id;
        }

        private bool IsLongitudeValid(string input)
        {
            if (double.TryParse(input, out double longitude))
            {
                return (longitude >= -180.0 && longitude <= 180.0);
            }
            return false;
        }

        private bool IsLatitudeValid(string input)
        {
            if (double.TryParse(input, out double latitude))
            {
                return (latitude >= -90.0 && latitude <= 90.0);
            }
            return false;
        }

        private bool IsValidOrientation(double input)
        {
            return (input >= -180.0 && input <= 180.0);
        }

        private bool IsValidIpAddressPart(int input)
        {
            return (input >= 0 && input <= 255);
        }

        private bool IsValidPort(int input)
        {
            return (input >= 0 && input <= 65535);
        }

        public bool IsValidated()
        {
            bool result = true;

            result &= IsLongitudeValid(Longitude);
            result &= IsLatitudeValid(Latitude);
            result &= IsValidOrientation(Orientation);
            result &= IsValidIpAddressPart(IpPart1);
            result &= IsValidIpAddressPart(IpPart2);
            result &= IsValidIpAddressPart(IpPart3);
            result &= IsValidIpAddressPart(IpPart4);
            result &= IsValidPort(Port);

            return result;
        }

        public RadarSetting ToRadarSettings()
        {
            if (!IsValidated())
            {
                return null;
            }

            return new RadarSetting()
            {
                Longitude = double.Parse(_longitude),
                Latitude = double.Parse(_latitude),
                Orientation = _orientation,
                Ip = $"{_ipPart1}.{_ipPart2}.{_ipPart3}.{_ipPart4}",
                Port = _port
            };
        }
    }
}
