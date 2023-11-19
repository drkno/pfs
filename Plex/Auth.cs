using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
        
        private class OAuthPin
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }
            [JsonPropertyName("code")]
            public string Code { get; set; }
        }

        private static async Task<OAuthPin> _GetPlexOAuthPin()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                foreach (var val in PlexHeaders)
                {
                    client.DefaultRequestHeaders.Add(val.Key, val.Value);
                }

                var response = await client.PostAsync("https://plex.tv/api/v2/pins?strong=true",
                    new ByteArrayContent(new byte[0]));
                return JsonSerializer.Deserialize<OAuthPin>(
                    await response.Content.ReadAsStreamAsync());
            }
        }

        private class OAuthTokenResponse
        {
            [JsonPropertyName("authToken")]
            public string AuthToken { get; set; }
        }
        
        private static async Task<string> _PerformLogin()
        {
            var oAuthPin = await _GetPlexOAuthPin();
            var url = $"https://app.plex.tv/auth/#!?clientID={PlexHeaders["X-Plex-Client-Identifier"]}&code={oAuthPin.Code}";
            try
            {
                Process.Start(url);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            Console.WriteLine(
                $"Please authenticate in your web browser. If your web browser did not open, please go to {url}");

            string token;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            foreach (var val in PlexHeaders)
            {
                client.DefaultRequestHeaders.Add(val.Key, val.Value);
            }

            var pinUrl = $"https://plex.tv/api/v2/pins/{oAuthPin.Id}";
            while (true)
            {
                try
                {
                    var response = await client.GetAsync(pinUrl);
                    var oAuthTokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(
                        await response.Content.ReadAsStreamAsync());
                    if (!string.IsNullOrWhiteSpace(oAuthTokenResponse.AuthToken))
                    {
                        token = oAuthTokenResponse.AuthToken;
                        break;
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e);
                    // user has not authed yet
                }
                
                Console.WriteLine("Sleeping for 5 seconds before checking again...");
                Thread.Sleep(5000);
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