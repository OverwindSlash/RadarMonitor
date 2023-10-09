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

        public DisplayConfigViewModel(Color scanlineColor, bool isFadingEnabled, int fadingInterval)
        {
            _scanlineColor = scanlineColor;
            _isFadingEnabled = isFadingEnabled;
            _fadingInterval = fadingInterval;
        }
    }
}
