using System;
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
        public string Cid { get; set; } = "PlexFSv1";

        [DefaultValue("")]
        [JsonProperty(PropertyName = "token", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Token { get; set; } = "";

        [DefaultValue(true)]
        [JsonProperty(PropertyName = "saveLoginDetails", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool SaveLoginDetails { get; set; } = true;

        [DefaultValue("")]
        [JsonProperty(PropertyName = "mountPath", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string MountPath { get; set; } = "";

        [DefaultValue(-1)]
        [JsonProperty(PropertyName = "uid", DefaultValueHandling = DefaultValueHandling.Populate)]
        public long Uid { get; set; } = -1;

        [DefaultValue(-1)]
        [JsonProperty(PropertyName = "gid", DefaultValueHandling = DefaultValueHandling.Populate)]
        public long Gid { get; set; } = -1;

        [DefaultValue(3600000)]
        [JsonProperty(PropertyName = "cacheAge", DefaultValueHandling = DefaultValueHandling.Populate)]
        public long CacheAge { get; set; } = 3600000;

        [DefaultValue(false)]
        [JsonProperty(PropertyName = "forceMount", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ForceMount { get; set; } = false;

        [DefaultValue(false)]
        [JsonProperty(PropertyName = "macDisplayMount", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool MacDisplayMount { get; set; } = false;

        [DefaultValue(new[] {"large_read"})]
        [JsonProperty(PropertyName = "fuseOptions", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string[] FuseOptions { get; set; } = {"large_read"};

        [JsonIgnore]
        private string ConfigurationFile { get; set; }

        private static IDictionary<string, IList<string>> ReadArgs()
        {
            var result = new Dictionary<string, IList<string>>();
            var argv = Environment.GetCommandLineArgs();
            string key = null;
            for (var i = 1; i < argv.Length; i++)
            {
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

        private static bool GetBool(string key, IList<string> input)
        {
            switch(input.Count)
            {
                case 0:
                    return true;
                case 1:
                    try
                    {
                        return bool.Parse(input[0]);
                    }
                    catch
                    {
                        throw new Exception($"Argument \"{key}\" expects at most one boolean argument but \"{input[0]}\" was given");
                    }
                default:
                    throw new Exception($"Argument \"{key}\" expects at most one boolean argument but {input.Count} were given");
            }
        }

        private static long GetLong(string key, IList<string> input)
        {
            if (input.Count != 1)
            {
                throw new Exception($"Argument \"{key}\" expects exactly one numeric argument but {input.Count} were given");
            }
            try
            {
                return long.Parse(input[0]);
            }
            catch
            {
                throw new Exception($"Argument \"{key}\" expects a numeric argument but \"{input[0]}\" was given");
            }
        }

        private static string GetString(string key, IList<string> input)
        {
            if (input.Count != 1)
            {
                throw new Exception($"Argument \"{key}\" expects exactly one argument but {input.Count} were given");
            }
            return input[0];
        }

        public static Configuration LoadConfig()
        {
            var cliArgs = ReadArgs();
            var configFile = cliArgs.ContainsKey("configFile") ? GetString("configFile", cliArgs["configFile"]) : "config.json";
            Configuration configuration;
            if (File.Exists(configFile))
            {
                using (var file = File.OpenText(configFile))
                {
                    using (var reader = new JsonTextReader(file))
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
                switch(key)
                {
                    case "cid":
                        configuration.Cid = GetString(key, cliArgs[key]);
                        break;
                    case "token":
                        configuration.Token = GetString(key, cliArgs[key]);
                        break;
                    case "saveLoginDetails":
                        configuration.SaveLoginDetails = GetBool(key, cliArgs[key]);
                        break;
                    case "mountPath":
                        configuration.MountPath = GetString(key, cliArgs[key]);
                        break;
                    case "uid":
                        configuration.Uid = GetLong(key, cliArgs[key]);
                        break;
                    case "gid":
                        configuration.Gid = GetLong(key, cliArgs[key]);
                        break;
                    case "cacheAge":
                        configuration.CacheAge = GetLong(key, cliArgs[key]);
                        break;
                    case "forceMount":
                        configuration.ForceMount = GetBool(key, cliArgs[key]);
                        break;
                    case "macDisplayMount":
                        configuration.MacDisplayMount = GetBool(key, cliArgs[key]);
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
                configuration.Uid = long.Parse(Environment.GetEnvironmentVariable("UID") ?? "0");
            }

            if (configuration.Gid < 0)
            {
                configuration.Gid = long.Parse(Environment.GetEnvironmentVariable("GID") ?? "0");
            }

            return configuration;
        }

        public static void SaveConfig(Configuration content)
        {
            using (var file = File.CreateText(content.ConfigurationFile))
            {
                var ser = new JsonSerializer { Formatting = Formatting.Indented };
                ser.Serialize(file, content);
            }
        }
    }
}
