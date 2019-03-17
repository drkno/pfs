using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pfs
{
    public class Configuration
    {
        [DefaultValue("PlexFSv1")]
        [JsonProperty(PropertyName = "cid", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Cid { get; set; }

        [DefaultValue("")]
        [JsonProperty(PropertyName = "token", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Token { get; set; }

        [DefaultValue(true)]
        [JsonProperty(PropertyName = "saveLoginDetails", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool SaveLoginDetails { get; set; }

        [DefaultValue("")]
        [JsonProperty(PropertyName = "mountPath", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string MountPath { get; set; }

        [DefaultValue(-1)]
        [JsonProperty(PropertyName = "uid", DefaultValueHandling = DefaultValueHandling.Populate)]
        public long Uid { get; set; }

        [DefaultValue(-1)]
        [JsonProperty(PropertyName = "gid", DefaultValueHandling = DefaultValueHandling.Populate)]
        public long Gid { get; set; }
        
        [DefaultValue(3600000)]
        [JsonProperty(PropertyName = "cacheAge", DefaultValueHandling = DefaultValueHandling.Populate)]
        public long CacheAge { get; set; }

        [DefaultValue(false)]
        [JsonProperty(PropertyName = "forceMount", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ForceMount { get; set; }

        [DefaultValue(false)]
        [JsonProperty(PropertyName = "macDisplayMount", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool MacDisplayMount { get; set; }

        [DefaultValue(new string[]{"large_read"})]
        [JsonProperty(PropertyName = "fuseOptions", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string[] FuseOptions { get; set; }

        [JsonIgnore]
        private string ConfigurationFile { get; set; }

        private static IDictionary<string, IList<string>> ReadArgs()
        {
            var result = new Dictionary<string, IList<string>>();
            var argv = Environment.GetCommandLineArgs();
            string key = null;
            for (var i = 1; i < argv.Length; i++) {
                if (argv[i].StartsWith('-'))
                {
                    var newKey = argv[i].TrimStart('-');
                    if (string.IsNullOrWhiteSpace(newKey) || result.ContainsKey(newKey))
                    {
                        throw new Exception($"Duplicate argument '{newKey}' provided");
                    }
                    result[newKey] = new List<string>();
                    key = newKey;
                }
                else if (string.IsNullOrWhiteSpace(key))
                {
                    throw new Exception($"Unexpected argument {argv[i]}");
                }
                else
                {
                    result[key].Add(argv[i]);
                }
            }
            return result;
        }

        private static bool GetBool(IList<string> input)
        {
            switch(input.Count) {
                case 0:
                    return true;
                case 1:
                    return bool.Parse(input[0]);
                default:
                    throw new Exception("Invalid number of arguments passed to a boolean input");
            }
        }

        private static long GetLong(IList<string> input)
        {
            if (input.Count != 1) {
                throw new Exception("Invalid number of arguments passed to a numeric input");
            }
            return long.Parse(input[0]);
        }

        private static string GetString(IList<string> input)
        {
            if (input.Count != 1) {
                throw new Exception("Invalid number of arguments passed to a string input");
            }
            return input[0];
        }

        public static Configuration LoadConfig()
        {
            var cliArgs = ReadArgs();
            var configFile = cliArgs.ContainsKey("configFile") ? GetString(cliArgs["configFile"]) : "config.json";
            Configuration configuration;
            if (File.Exists(configFile))
            {
                using (var file = File.OpenText(configFile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        configuration = JToken.ReadFrom(reader).ToObject<Configuration>();
                    }
                }
            }
            else
            {
                configuration = new Configuration();
            }
            configuration.ConfigurationFile = configFile;

            foreach (var key in cliArgs.Keys)
            {
                switch(key) {
                    case "cid":
                        configuration.Cid = GetString(cliArgs[key]);
                        break;
                    case "token":
                        configuration.Token = GetString(cliArgs[key]);
                        break;
                    case "saveLoginDetails":
                        configuration.SaveLoginDetails = GetBool(cliArgs[key]);
                        break;
                    case "mountPath":
                        configuration.MountPath = cliArgs[key][0];
                        break;
                    case "uid":
                        configuration.Uid = GetLong(cliArgs[key]);
                        break;
                    case "gid":
                        configuration.Gid = GetLong(cliArgs[key]);
                        break;
                    case "cacheAge":
                        configuration.CacheAge = GetLong(cliArgs[key]);
                        break;
                    case "forceMount":
                        configuration.ForceMount = GetBool(cliArgs[key]);
                        break;
                    case "macDisplayMount":
                        configuration.MacDisplayMount = GetBool(cliArgs[key]);
                        break;
                    case "fuseOptions":
                        configuration.FuseOptions = new string[cliArgs[key].Count];
                        cliArgs[key].CopyTo(configuration.FuseOptions, 0);
                        break;
                    default:
                        throw new Exception($"Unknown argument {key}");
                }
            }

            if (configuration.Uid < 0)
            {
                configuration.Uid = long.Parse(Environment.GetEnvironmentVariable("UID"));
            }

            if (configuration.Gid < 0)
            {
                configuration.Gid = long.Parse(Environment.GetEnvironmentVariable("GID"));
            }

            return configuration;
        }

        public static void SaveConfig(Configuration content)
        {
            using (var file = File.CreateText(content.ConfigurationFile))
            {
                var ser = new JsonSerializer() { Formatting = Formatting.Indented };
                ser.Serialize(file, content);
            }
        }
    }
}
