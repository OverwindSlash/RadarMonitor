using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RadarMonitor.Model
{
    public class UserConfiguration
    {
        public EncSetting EncSetting { get; set; }
        public List<RadarSetting> RadarSettings { get; set; }

        public UserConfiguration()
        {
            EncSetting = new EncSetting();
            RadarSettings = new List<RadarSetting>();
        }

        public static UserConfiguration LoadConfiguration(string configFileName)
        {
            try
            {
                string contents = File.ReadAllText(configFileName);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var configuration = deserializer.Deserialize<UserConfiguration>(contents);

                return configuration;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return new UserConfiguration();
            }
        }

        public static void SaveConfiguration(UserConfiguration configuration, string configFileName)
        {
            if (configuration == null)
            {
                return;
            }

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var content = serializer.Serialize(configuration);

            File.WriteAllText(configFileName, content);
        }
    }
}
