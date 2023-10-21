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

        protected bool SetField<T>(T field, T value, [CallerMemberName] string? propertyName = null)
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        private List<RadarSetting> _radarSettings;
        private RadarSetting _currentEditRadarSetting;

        #region Radar Properties
        public List<RadarSetting> RadarSettings
        {
            get => _radarSettings;
            set
            {
                SetField(ref _radarSettings, value, "RadarSettings");
            }
        }

        public RadarSetting CurrentEditRadarSetting
        {
            get => _currentEditRadarSetting;
            set
            {
                SetField(ref _currentEditRadarSetting, value, "CurrentEditRadarSetting");
                Name = _currentEditRadarSetting.RadarName;
                Enabled = _currentEditRadarSetting.RadarEnabled;
                Longitude = _currentEditRadarSetting.RadarLongitude;
                Latitude = _currentEditRadarSetting.RadarLatitude;
                Orientation = _currentEditRadarSetting.RadarOrientation;
                MaxDistance = _currentEditRadarSetting.RadarMaxDistance;
                IpPart1 = _currentEditRadarSetting.RadarIpAddress.Split('.')[0];
                IpPart2 = _currentEditRadarSetting.RadarIpAddress.Split('.')[1];
                IpPart3 = _currentEditRadarSetting.RadarIpAddress.Split('.')[2];
                IpPart4 = _currentEditRadarSetting.RadarIpAddress.Split('.')[3];
                Port = _currentEditRadarSetting.RadarPort;
            }
        }

        public string Name
        {
            get => _currentEditRadarSetting.RadarName;
            set
            {
                SetField(_currentEditRadarSetting.RadarName, value, "Name");
            }
        }

        public bool Enabled
        {
            get => _currentEditRadarSetting.RadarEnabled;
            set
            {
                SetField(_currentEditRadarSetting.RadarEnabled, value, "Enabled");
            }
        }

        public double Longitude
        {
            get => _currentEditRadarSetting.RadarLongitude;
            set
            {
                SetField(_currentEditRadarSetting.RadarLongitude, value, "Longitude");
            }
        }

        public double Latitude
        {
            get => _currentEditRadarSetting.RadarLatitude;
            set
            {
                SetField(_currentEditRadarSetting.RadarLatitude, value, "Latitude");
            }
        }

        public double Orientation
        {
            get => _currentEditRadarSetting.RadarOrientation;
            set
            {
                SetField(_currentEditRadarSetting.RadarOrientation, value, "Orientation");
            }
        }

        public double MaxDistance
        {
            get => _currentEditRadarSetting.RadarMaxDistance;
            set
            {
                SetField(_currentEditRadarSetting.RadarMaxDistance, value, "MaxDistance");
            }
        }

        public string IpPart1
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[0];
            set
            {
                string ip = $"{value}.{IpPart2}.{IpPart3}.{IpPart4}";
                SetField(_currentEditRadarSetting.RadarIpAddress, ip, "IpPart1");
            }
        }

        public string IpPart2
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[1];
            set
            {
                string ip = $"{IpPart1}.{value}.{IpPart3}.{IpPart4}";
                SetField(_currentEditRadarSetting.RadarIpAddress, ip, "IpPart2");
            }
        }

        public string IpPart3
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[2];
            set
            {
                string ip = $"{IpPart1}.{IpPart2}.{value}.{IpPart4}";
                SetField(_currentEditRadarSetting.RadarIpAddress, ip, "IpPart3");
            }
        }

        public string IpPart4
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[3];
            set
            {
                string ip = $"{IpPart1}.{IpPart2}.{IpPart3}.{value}";
                SetField(_currentEditRadarSetting.RadarIpAddress, ip, "IpPart4");
            }
        }

        public int Port
        {
            get => _currentEditRadarSetting.RadarPort;
            set
            {
                SetField(_currentEditRadarSetting.RadarPort, value, "Port");
            }
        }
        #endregion


        public RadarSettingsViewModel(List<RadarSetting> radarSettings)
        {
            RadarSettings = radarSettings;
            CurrentEditRadarSetting = RadarSettings[0];
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

            // result &= IsLongitudeValid(Longitude);
            // result &= IsLatitudeValid(Latitude);
            // result &= IsValidOrientation(Orientation);
            // result &= IsValidIpAddressPart(IpPart1);
            // result &= IsValidIpAddressPart(IpPart2);
            // result &= IsValidIpAddressPart(IpPart3);
            // result &= IsValidIpAddressPart(IpPart4);
            // result &= IsValidPort(Port);

            return result;
        }
    }
}
