using YamlDotNet.Serialization;

namespace RadarMonitor.Model
{
    public class RadarSetting
    {
        public string RadarName { get; set; } = @"Radar";

        public double RadarLongitude { get; set; } = 0;
        public double RadarLatitude { get; set; } = 0;
        public double RadarScale { get; internal set; } = 500000;

        public double RadarOrientation { get; set; } = 0;
        public int RadarMaxDistance { get; set; } = 0;

        public string RadarIpAddress { get; set; } = "127.0.0.1";
        public int RadarPort { get; set; } = 20101;

        public bool IsRadarEnabled { get; set; } = false;

        [YamlIgnore]
        public bool IsConnected { get; set; } = false;
        public bool IsRingsDisplayed { get; set; } = false;
        public bool IsEchoDisplayed { get; set; } = false;
        public bool IsOpenGlEchoDisplayed { get; set; } = false;
    }
}
