using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pfs;

namespace Pfs.Plex
{
    public class BaseClient : IDisposable
    {
        private string _token;
        private string _cid;
        private HttpClient _client;
        private Memoriser _memoriser;

        public BaseClient(Configuration config)
        {
            this._token = config.Token;
            this._cid = config.Cid;
            this._client = new HttpClient();
            this._client.DefaultRequestHeaders.Accept.Clear();
            this._client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this._memoriser = new Memoriser(config);
        }

        private string _BuildPlexUrl(string server, string path, IDictionary<string, string> urlParams)
        {
            if (!urlParams.ContainsKey("X-Plex-Token"))
            {
                urlParams["X-Plex-Token"] = this._token;
            }
            if (!urlParams.ContainsKey("X-Plex-Client-Identifier"))
            {
                urlParams["X-Plex-Client-Identifier"] = this._cid;
            }

            return server + path + "?" + urlParams
                .Select(p => p.Key + "=" + p.Value)
                .Aggregate("", (current, next) => current + "&" + next);
        }

        public async Task<T> JsonFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            return await this._memoriser.Memorise(this._InternalJsonFetch<T>, server, path, urlParams);
        }

        private async Task<T> _InternalJsonFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            var response = await this._client.GetAsync(this._BuildPlexUrl(server, path, urlParams));
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
            return await this._memoriser.Memorise(this._InternalXmlFetch<T>, server, path, urlParams);
        }

        private async Task<T> _InternalXmlFetch<T>(string server, string path, IDictionary<string, string> urlParams)
        {
            var response = await this._client.GetAsync(this._BuildPlexUrl(server, path, urlParams));
            response.EnsureSuccessStatusCode();
            return _ToXml<T>(await response.Content.ReadAsStreamAsync());
        }

        public async Task<string> HeadFetch(string uri)
        {
            return await this._memoriser.Memorise(this._InternalHeadFetch, uri);
        }

        private async Task<string> _InternalHeadFetch(string uri)
        {
            using (var headClient = new HttpClient())
            {
                headClient.Timeout = TimeSpan.FromMilliseconds(500);
                try
                {
                    await this._client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                }
                catch(TaskCanceledException)
                {
                    throw new Exception();
                }
            }
            return uri;
        }

        // async bufferFetch(server, path, params, startIndex, endIndex) {
        //     return await (await fetch(this._buildPlexUrl(server, path, params), {
        //         headers: {
        //             'Accept': 'application/json',
        //             'Range': `bytes=${startIndex}-${endIndex}`
        //         }
        //     })).buffer();
        // }

        public void Dispose()
        {
            this._client.Dispose();
        }
    }
}

