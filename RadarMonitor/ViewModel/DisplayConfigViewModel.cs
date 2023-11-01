using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace RadarMonitor.ViewModel
{
    public delegate void EchoColorChangedEventHandler(object sender, int radarId, Color scanlineColor);
    public delegate void FadingEnabledChangedEventHandler(object sender, int radarId, bool isFadingEnabled);
    public delegate void FadingIntervalChangedEventHandler(object sender, int radarId, int fadingInterval);
    public delegate void EchoThresholdChangedEventHandler(object sender, int radarId, double echoThreshold);
    public delegate void EchoRadiusChangedEventHandler(object sender, int radarId, double echoRadius);


    public class DisplayConfigViewModel : INotifyPropertyChanged
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

        private int _radarId;
        private Color _scanlineColor;
        private bool _isFadingEnabled;
        private int _fadingInterval;
        private double _echoThreshold;
        private double _echoRadius;
        private double _echoMaxDistance;

        public event EchoColorChangedEventHandler OnEchoColorChanged;
        public event FadingEnabledChangedEventHandler OnFadingEnabledChanged;
        public event FadingIntervalChangedEventHandler OnFadingIntervalChanged;
        public event EchoThresholdChangedEventHandler OnEchoThresholdChanged;
        public event EchoRadiusChangedEventHandler OnEchoRadiusChanged;

        public Color ScanlineColor
        {
            get => _scanlineColor;
            set
            {
                SetField(ref _scanlineColor, value, "ScanlineColor");
                OnEchoColorChanged?.Invoke(this, _radarId, _scanlineColor);
            }
        }

        public bool IsFadingEnabled
        {
            get => _isFadingEnabled;
            set
            {
                SetField(ref _isFadingEnabled, value, "IsFadingEnabled");
                OnFadingEnabledChanged?.Invoke(this, _radarId, _isFadingEnabled);
            }
        }

        public int FadingInterval
        {
            get => _fadingInterval;
            set
            {
                if (value > 0)
                {
                    SetField(ref _fadingInterval, value, "FadingInterval");
                }
                else
                {
                    SetField(ref _fadingInterval, 1, "FadingInterval");
                }

                OnFadingIntervalChanged?.Invoke(this, _radarId, _fadingInterval);
            }
        }

        public double EchoThreshold
        {
            get => _echoThreshold;
            set
            {
                if (value >= 0 && value <= 1)
                {
                    SetField(ref _echoThreshold, value, "EchoThreshold");
                    OnEchoThresholdChanged?.Invoke(this, _radarId, _echoThreshold);
                }
            }
        }

        public double EchoRadius
        {
            get => _echoRadius;
            set
            {
                if (_echoRadius >= 0)
                {
                    SetField(ref _echoRadius, value, "EchoRadius");
                    OnEchoRadiusChanged?.Invoke(this, _radarId, _echoRadius);
                }
            }
        }

        public double EchoMaxDistance
        {
            get => _echoMaxDistance;
            set
            {
                if (_echoMaxDistance >= 0)
                {
                    SetField(ref _echoMaxDistance, value, "EchoMaxDistance");
                }
            }
        }

        public DisplayConfigViewModel(int radarId, Color scanlineColor, bool isFadingEnabled, int fadingInterval, 
            double echoThreshold, double echoRadius, double echoMaxDistance)
        {
            _radarId = radarId;
            ScanlineColor = scanlineColor;
            IsFadingEnabled = isFadingEnabled;
            FadingInterval = fadingInterval;
            EchoThreshold = echoThreshold;
            EchoRadius = echoRadius;
            EchoMaxDistance = echoMaxDistance;
        }
    }
}
