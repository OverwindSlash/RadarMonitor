using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace RadarMonitor.ViewModel
{
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

        private Color _scanlineColor;
        private bool _isFadingEnabled;
        private int _fadingInterval;
        private double _echoThreshold;
        private double _echoRadius;
        private double _echoMaxDistance;

        public Color ScanlineColor
        {
            get => _scanlineColor;
            set
            {
                SetField(ref _scanlineColor, value, "ScanlineColor");
            }
        }

        public bool IsFadingEnabled
        {
            get => _isFadingEnabled;
            set
            {
                SetField(ref _isFadingEnabled, value, "IsFadingEnabled");
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

        public DisplayConfigViewModel(Color scanlineColor, bool isFadingEnabled, int fadingInterval, 
            double echoThreshold, double echoRadius, double echoMaxDistance)
        {
            ScanlineColor = scanlineColor;
            IsFadingEnabled = isFadingEnabled;
            FadingInterval = fadingInterval;
            EchoThreshold = echoThreshold;
            EchoRadius = echoRadius;
            EchoMaxDistance = echoMaxDistance;
        }
    }
}
