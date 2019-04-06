using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pfs;

namespace Pfs.Plex.Api
{
    public class BaseApiClient : IDisposable
    {
        private readonly string _token;
        private readonly string _cid;
        private readonly HttpClient _client;
        private readonly Memoriser _memoriser;

        public BaseApiClient(Configuration config)
        {
            _token = config.Token;
            _cid = config.Cid;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _memoriser = new Memoriser(config);
            ServicePointManager.DefaultConnectionLimit = 50;
        }

        private string _BuildPlexUrl(string server, string path, IDictionary<string, string> urlParams)
        {
            if (!urlParams.ContainsKey("X-Plex-Token"))
            {
                urlParams["X-Plex-Token"] = _token;
            }
            if (!urlParams.ContainsKey("X-Plex-Client-Identifier"))
            {
                urlParams["X-Plex-Client-Identifier"] = _cid;
            }

            return server + path + "?" + urlParams
                .Select(p => p.Key + "=" + p.Value)
                .Aggregate("", (current, next) => current.Length == 0 ? next : (current + "&" + next));
        }

        public async Task<T> JsonFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            return await _memoriser.Memorise(_InternalJsonFetch<T>, server, path, urlParams);
        }

        private async Task<T> _InternalJsonFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            var response = await _client.GetAsync(_BuildPlexUrl(server, path, urlParams));
            response.EnsureSuccessStatusCode();
            using (var reader = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())))
            {
                return JToken.ReadFrom(reader).ToObject<T>();
            }
        }

        private T _ToXml<T>(Stream inputXmlStream)
        {
            var serializer = new XmlSerializer(typeof(T)); 
            return (T) serializer.Deserialize(inputXmlStream); 
        }

        public async Task<T> XmlFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            return await _memoriser.Memorise(_InternalXmlFetch<T>, server, path, urlParams);
        }

        private async Task<T> _InternalXmlFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            var response = await _client.GetAsync(_BuildPlexUrl(server, path, urlParams));
            response.EnsureSuccessStatusCode();
            return _ToXml<T>(await response.Content.ReadAsStreamAsync());
        }

        public async Task<string> HeadFetch(string uri)
        {
            return await _memoriser.Memorise(_InternalHeadFetch, uri);
        }

        private async Task<string> _InternalHeadFetch(string uri)
        {
            using (var headClient = new HttpClient())
            {
                headClient.Timeout = TimeSpan.FromMilliseconds(500);
                try
                {
                    await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                }
                catch(TaskCanceledException)
                {
                    throw new Exception();
                }
            }
            return uri;
        }

        public async Task<long> BufferFetch(string server, string path, IDictionary<string, string> urlParams, long startIndex, long endIndex, byte[] outputBuffer)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, _BuildPlexUrl(server, path, urlParams)))
            {
                request.Headers.Add("Range", $"bytes={startIndex}-{endIndex}");
                using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var sourceStream = await response.Content.ReadAsStreamAsync())
                    {
                        var maxLength = (int) Math.Min(response.Content.Headers.ContentLength ?? long.MaxValue, outputBuffer.Length);
                        var read = 0;
                        var lastNumberOfBytes = 0;
                        do
                        {
                            lastNumberOfBytes = await sourceStream.ReadAsync(outputBuffer, read, maxLength - read);
                            read += lastNumberOfBytes;
                        }
                        while (lastNumberOfBytes > 0 && read < maxLength);
                        return Math.Max(read, 0);
                    }
                }
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}

