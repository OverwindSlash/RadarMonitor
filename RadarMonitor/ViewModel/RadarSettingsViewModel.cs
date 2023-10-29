using RadarMonitor.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

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

        private List<RadarSetting> _radarSettings;
        private RadarSetting _currentEditRadarSetting;

        private bool _isLongitudeValid;
        private bool _isLatitudeValid;
        private bool _isOrientationValid;
        private bool _isMaxDistanceValid;
        private bool _isIpPart1Valid;
        private bool _isIpPart2Valid;
        private bool _isIpPart3Valid;
        private bool _isIpPart4Valid;
        private bool _isIpPortValid;

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
                Enabled = _currentEditRadarSetting.IsRadarEnabled;
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
                _currentEditRadarSetting.RadarName = value;
                OnPropertyChanged("Name");
            }
        }

        public bool Enabled
        {
            get => _currentEditRadarSetting.IsRadarEnabled;
            set
            {
                _currentEditRadarSetting.IsRadarEnabled = value;
                OnPropertyChanged("Enabled");
            }
        }

        public double Longitude
        {
            get => _currentEditRadarSetting.RadarLongitude;
            set
            {
                _currentEditRadarSetting.RadarLongitude = value;
                OnPropertyChanged("Longitude");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsLongitudeValid
        {
            get => _isLongitudeValid;
            set
            {
                SetField(ref _isLongitudeValid, value, "IsLongitudeValid");
            }
        }

        public double Latitude
        {
            get => _currentEditRadarSetting.RadarLatitude;
            set
            {
                _currentEditRadarSetting.RadarLatitude = value;
                OnPropertyChanged("Latitude");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsLatitudeValid
        {
            get => _isLatitudeValid;
            set
            {
                SetField(ref _isLatitudeValid, value, "IsLatitudeValid");
            }
        }

        public double Orientation
        {
            get => _currentEditRadarSetting.RadarOrientation;
            set
            {
                _currentEditRadarSetting.RadarOrientation = value;
                OnPropertyChanged("Orientation");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsOrientationValid
        {
            get => _isOrientationValid;
            set
            {
                SetField(ref _isOrientationValid, value, "IsOrientationValid");
            }
        }

        public int MaxDistance
        {
            get => _currentEditRadarSetting.RadarMaxDistance;
            set
            {
                _currentEditRadarSetting.RadarMaxDistance = value;
                OnPropertyChanged("MaxDistance");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsMaxDistanceValid
        {
            get => _isMaxDistanceValid;
            set
            {
                SetField(ref _isMaxDistanceValid, value, "IsMaxDistanceValid");
            }
        }

        public string IpPart1
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[0];
            set
            {
                string ip = $"{value}.{IpPart2}.{IpPart3}.{IpPart4}";
                _currentEditRadarSetting.RadarIpAddress = ip;
                OnPropertyChanged("IpPart1");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsIpPart1Valid
        {
            get => _isIpPart1Valid;
            set
            {
                SetField(ref _isIpPart1Valid, value, "IsIpPart1Valid");
            }
        }

        public string IpPart2
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[1];
            set
            {
                string ip = $"{IpPart1}.{value}.{IpPart3}.{IpPart4}";
                _currentEditRadarSetting.RadarIpAddress = ip;
                OnPropertyChanged("IpPart2");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsIpPart2Valid
        {
            get => _isIpPart2Valid;
            set
            {
                SetField(ref _isIpPart2Valid, value, "IsIpPart2Valid");
            }
        }

        public string IpPart3
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[2];
            set
            {
                string ip = $"{IpPart1}.{IpPart2}.{value}.{IpPart4}";
                _currentEditRadarSetting.RadarIpAddress = ip;
                OnPropertyChanged("IpPart3");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsIpPart3Valid
        {
            get => _isIpPart3Valid;
            set
            {
                SetField(ref _isIpPart3Valid, value, "IsIpPart3Valid");
            }
        }

        public string IpPart4
        {
            get => _currentEditRadarSetting.RadarIpAddress.Split('.')[3];
            set
            {
                string ip = $"{IpPart1}.{IpPart2}.{IpPart3}.{value}";
                _currentEditRadarSetting.RadarIpAddress = ip;
                OnPropertyChanged("IpPart4");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsIpPart4Valid
        {
            get => _isIpPart4Valid;
            set
            {
                SetField(ref _isIpPart4Valid, value, "IsIpPart4Valid");
            }
        }

        public int Port
        {
            get => _currentEditRadarSetting.RadarPort;
            set
            {
                _currentEditRadarSetting.RadarPort = value;
                OnPropertyChanged("Port");
                OnPropertyChanged("IsRadarSettingValid");
            }
        }

        public bool IsIpPortValid
        {
            get => _isIpPortValid;
            set
            {
                SetField(ref _isIpPortValid, value, "IsIpPortValid");
            }
        }

        public bool IsRadarSettingValid
        {
            get => ValidateRadarSetting();
        }
        #endregion

        public RadarSettingsViewModel(List<RadarSetting> radarSettings)
        {
            RadarSettings = radarSettings;
            CurrentEditRadarSetting = RadarSettings[0];
        }

        public bool ValidateRadarSetting()
        {
            return _isLongitudeValid && _isLatitudeValid && _isOrientationValid && _isOrientationValid && 
                   _isIpPart1Valid && _isIpPart2Valid && _isIpPart3Valid && _isIpPart4Valid && _isIpPortValid;
        }
    }

    public class LongitudeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!double.TryParse(input, out double longitude))
            {
                viewModel.IsLongitudeValid = false;
                return new ValidationResult(false, "Longitude must be a number.");
            }

            if (longitude < -180.0 || longitude > 180.0)
            {
                viewModel.IsLongitudeValid = false;
                return new ValidationResult(false, "Longitude must between -180.0 and 180.0.");
            }

            viewModel.IsLongitudeValid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class LatitudeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!double.TryParse(input, out double latitude))
            {
                viewModel.IsLatitudeValid = false;
                return new ValidationResult(false, "Latitude must be a number.");
            }

            if (latitude < -90.0 || latitude > 90.0)
            {
                viewModel.IsLatitudeValid = false;
                return new ValidationResult(false, "Latitude must between -90.0 and 90.0.");
            }

            viewModel.IsLatitudeValid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class OrientationValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!double.TryParse(input, out double orientation))
            {
                viewModel.IsOrientationValid = false;
                return new ValidationResult(false, "Orientation must be a number.");
            }

            if (orientation < -180.0 || orientation > 180.0)
            {
                viewModel.IsOrientationValid = false;
                return new ValidationResult(false, "Longitude must between -180.0 and 180.0.");
            }

            viewModel.IsOrientationValid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class MaxDistanceValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!double.TryParse(input, out double distance))
            {
                viewModel.IsMaxDistanceValid = false;
                return new ValidationResult(false, "Max distance must be a number.");
            }

            if (distance < 0.0 || distance > 200.0)
            {
                viewModel.IsMaxDistanceValid = false;
                return new ValidationResult(false, "Max distance must between 0 and 200.0.");
            }

            viewModel.IsMaxDistanceValid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class IpPart1ValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!int.TryParse(input, out int ipPart))
            {
                viewModel.IsIpPart1Valid = false;
                return new ValidationResult(false, "Ip must be a number.");
            }

            if (ipPart < 0 || ipPart > 255)
            {
                viewModel.IsIpPart1Valid = false;
                return new ValidationResult(false, "Ip must between 0 and 255.");
            }

            viewModel.IsIpPart1Valid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class IpPart2ValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!int.TryParse(input, out int ipPart))
            {
                viewModel.IsIpPart2Valid = false;
                return new ValidationResult(false, "Ip must be a number.");
            }

            if (ipPart < 0 || ipPart > 255)
            {
                viewModel.IsIpPart2Valid = false;
                return new ValidationResult(false, "Ip must between 0 and 255.");
            }

            viewModel.IsIpPart2Valid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class IpPart3ValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!int.TryParse(input, out int ipPart))
            {
                viewModel.IsIpPart3Valid = false;
                return new ValidationResult(false, "Ip must be a number.");
            }

            if (ipPart < 0 || ipPart > 255)
            {
                viewModel.IsIpPart3Valid = false;
                return new ValidationResult(false, "Ip must between 0 and 255.");
            }

            viewModel.IsIpPart3Valid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class IpPart4ValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            var viewModel = ((MainWindow)Application.Current.MainWindow).DialogViewModel;

            if (!int.TryParse(input, out int ipPart))
            {
                viewModel.IsIpPart4Valid = false;
                return new ValidationResult(false, "Ip must be a number.");
            }

            if (ipPart < 0 || ipPart > 255)
            {
                viewModel.IsIpPart4Valid = false;
                return new ValidationResult(false, "Ip must between 0 and 255.");
            }

            viewModel.IsIpPart4Valid = true;
            return ValidationResult.ValidResult;
        }
    }

    public class IpPortValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string input = value as string;

            if (!int.TryParse(input, out int port))
            {
                return new ValidationResult(false, "Port must be a number.");
            }

            if (port < 0 || port > 65535)
            {
                return new ValidationResult(false, "Port must between 0 and 65535.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
