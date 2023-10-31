using System;

namespace RadarMonitor.Model
{
    public class EncSetting
    {
        public string EncType { get; set; } = "Catalog";
        public string EncUri { get; set; } = @"Enc\CATALOG.031";
        public double EncLongitude { get; set; } = 0;
        public double EncLatitude { get; set; } = 0;
        public double EncScale { get; set; } = 500000;
        public bool EncEnabled { get; set; } = false;

        public EncSetting()
        {
        }

        protected bool Equals(EncSetting other)
        {
            return EncType == other.EncType 
                   && EncUri == other.EncUri 
                   && EncLongitude.Equals(other.EncLongitude) 
                   && EncLatitude.Equals(other.EncLatitude) 
                   && EncScale.Equals(other.EncScale) 
                   && EncEnabled == other.EncEnabled;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EncSetting)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EncType, EncUri, EncLongitude, EncLatitude, EncScale, EncEnabled);
        }
    }
}
