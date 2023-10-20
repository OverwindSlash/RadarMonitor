using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RadarMonitor.Model
{
    public class UserConfiguration
    {
        public EncSettings EncConfiguration { get; set; }
        public List<RadarSettings> RadarConfigurations { get; set; }

        public UserConfiguration()
        {
            EncConfiguration = new EncSettings();
            RadarConfigurations = new List<RadarSettings>();
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
