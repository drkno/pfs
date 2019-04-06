using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pfs.Plex
{
    public static class PlexOAuth
    {
        private static readonly IDictionary<string, string> PlexHeaders = new Dictionary<string, string>
        {
            { "X-Plex-Product", "PlexFS" },
            { "X-Plex-Version", "PlexFS" },
            { "X-Plex-Client-Identifier", "PlexFSv1" }
        };

        private static async Task<(string, string)> _GetPlexOAuthPin()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                foreach (var val in PlexHeaders)
                {
                    client.DefaultRequestHeaders.Add(val.Key, val.Value);
                }

                var response = await client.PostAsync("https://plex.tv/api/v2/pins?strong=true", new ByteArrayContent(new byte[0]));
                using (var reader = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())))
                {
                    var json = JToken.ReadFrom(reader);
                    return ((string)json["id"], (string)json["code"]);
                }
            }
        }

        private static async Task<string> _PerformLogin()
        {
            var (pin, code) = await _GetPlexOAuthPin();
            var url = $"https://app.plex.tv/auth/#!?clientID={PlexHeaders["X-Plex-Client-Identifier"]}&code={code}";
            try
            {
                Process.Start(url);
            }
            catch(Exception) {}
            Console.WriteLine($"Please authenticate in your web browser. If your web browser did not open, please go to {url}");
            
            string token;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                foreach (var val in PlexHeaders)
                {
                    client.DefaultRequestHeaders.Add(val.Key, val.Value);
                }

                var pinUrl = $"https://plex.tv/api/v2/pins/{pin}";
                while (true)
                {
                    var response = await client.GetAsync(pinUrl);
                    using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                    {
                        try
                        {
                            var data = reader.ReadToEnd();
                            var json = JObject.Parse(data);
                            if (!string.IsNullOrWhiteSpace(json["authToken"].ToString()))
                            {
                                token = (string) json["authToken"];
                                break;
                            }
                        }
                        catch
                        {
                            // user has not authed yet
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
            
            return token;
        }

        public static async Task<(string, string)> GetLoginDetails()
        {
            Console.WriteLine("Login Required.");
            var token = await _PerformLogin();
            Console.WriteLine("Login Complete. Plex token received.");
            return (PlexHeaders["X-Plex-Client-Identifier"], token);
        }
    }
}
