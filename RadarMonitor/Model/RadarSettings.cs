using System;

namespace RadarMonitor.Model
{
    public class RadarSettings
    {
        public string RadarName { get; set; } = @"Radar";
        public double RadarLongitude { get; set; } = 0;
        public double RadarLatitude { get; set; } = 0;
        public double RadarOrientation { get; set; } = 0;
        public double RadarScale { get; internal set; } = 500000;
        public string RadarIpAddress { get; set; } = "127.0.0.1";
        public int RadarPort { get; set; } = 20101;
        public bool RadarEnabled { get; set; } = false;

        public RadarSettings()
        {
        }

        protected bool Equals(RadarSettings other)
        {
            return RadarName == other.RadarName 
                   && RadarLongitude.Equals(other.RadarLongitude) 
                   && RadarLatitude.Equals(other.RadarLatitude) 
                   && RadarOrientation.Equals(other.RadarOrientation) 
                   && RadarIpAddress == other.RadarIpAddress 
                   && RadarPort == other.RadarPort 
                   && RadarEnabled == other.RadarEnabled;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RadarSettings)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RadarName, RadarLongitude, RadarLatitude, RadarOrientation, RadarIpAddress, RadarPort, RadarEnabled);
        }
    }
}
