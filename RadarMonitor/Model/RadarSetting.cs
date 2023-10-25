using System;

namespace RadarMonitor.Model
{
    public class RadarSetting
    {
        public int Id { get; set; } = 0;
        public double Longitude { get; set; } = 0;
        public double Latitude { get; set; } = 0;
        public double Orientation { get; set; } = 0;
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 20101;

        protected bool Equals(RadarSetting other)
        {
            return Longitude.Equals(other.Longitude) && Latitude.Equals(other.Latitude) 
                                                     && Orientation.Equals(other.Orientation) 
                                                     && Ip == other.Ip 
                                                     && Port == other.Port;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RadarSetting)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Longitude, Latitude, Orientation, Ip, Port);
        }
    }
}
