using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Pfs.Plex.Apis
{
    public class ServersClient
    {
        private BaseClient _client;
        private Random _random;

        public ServersClient(BaseClient client)
        {
            this._client = client;
            this._random = new Random();
        }

        private async Task<string> _FindServer(IEnumerable<string> uris)
        {
            var tasks = new List<Task<string>>();
            foreach (var uri in uris)
            {
                tasks.Add(this._client.HeadFetch(uri));
            }
            return await Utils.FirstSuccessfulTask<string>(tasks);
        }

        private async Task<Server> ToServer(Device device)
        {
            var url = await this._FindServer(device.Connection.Select(c => c.Uri));
            if (url == null) {
                return null;
            }
            return new Server()
            {
                Name = device.Name,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(device.CreatedAt)).DateTime,
                LastModified = DateTimeOffset.FromUnixTimeSeconds(long.Parse(device.LastSeenAt)).DateTime,
                Token = device.AccessToken,
                Url = url,
                Type = FileType.Folder,
                Id = this._random.Next()
            };
        }

        public async Task<IEnumerable<Server>> ListServers() {
            var servers = await this._client.XmlFetch<MediaContainer>("https://plex.tv", "/api/resources", new Dictionary<string, string>()
            {
                { "includeHttps", "1" },
                { "includeRelay", "1" }
            });

            var serverCount = servers.Device?.Count ?? 0;
            if (serverCount == 0)
            {
                Console.WriteLine("You do not have access to any plex servers!");
                return new List<Server>();
            }

            if ((servers.Device == null || int.Parse(servers.Size) != servers.Device.Count) && Environment.GetEnvironmentVariable("DEBUG") != null)
            {
                Console.WriteLine($"User should have access to {servers.Size} servers, but none were returned.");
            }

            var filtered = (await Task.WhenAll(servers.Device
                .Where(d => d.Presence == "1")
                .Select(ToServer)))
                .Where(d => !string.IsNullOrWhiteSpace(d?.Url));
            
            Utils.CleanAndDedupe(filtered);
            
            return filtered;
        }

        [XmlRoot(ElementName="MediaContainer")]
        public class MediaContainer
        {
            [XmlElement(ElementName="Device")]
            public List<Device> Device { get; set; }
            [XmlAttribute(AttributeName="size")]
            public string Size { get; set; }
        }

        [XmlRoot(ElementName="Device")]
        public class Device
        {
            [XmlElement(ElementName="Connection")]
            public List<Connection> Connection { get; set; }
            [XmlAttribute(AttributeName="name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName="product")]
            public string Product { get; set; }
            [XmlAttribute(AttributeName="productVersion")]
            public string ProductVersion { get; set; }
            [XmlAttribute(AttributeName="platform")]
            public string Platform { get; set; }
            [XmlAttribute(AttributeName="platformVersion")]
            public string PlatformVersion { get; set; }
            [XmlAttribute(AttributeName="device")]
            public string DeviceName { get; set; }
            [XmlAttribute(AttributeName="clientIdentifier")]
            public string ClientIdentifier { get; set; }
            [XmlAttribute(AttributeName="createdAt")]
            public string CreatedAt { get; set; }
            [XmlAttribute(AttributeName="lastSeenAt")]
            public string LastSeenAt { get; set; }
            [XmlAttribute(AttributeName="provides")]
            public string Provides { get; set; }
            [XmlAttribute(AttributeName="owned")]
            public string Owned { get; set; }
            [XmlAttribute(AttributeName="accessToken")]
            public string AccessToken { get; set; }
            [XmlAttribute(AttributeName="publicAddress")]
            public string PublicAddress { get; set; }
            [XmlAttribute(AttributeName="httpsRequired")]
            public string HttpsRequired { get; set; }
            [XmlAttribute(AttributeName="synced")]
            public string Synced { get; set; }
            [XmlAttribute(AttributeName="relay")]
            public string Relay { get; set; }
            [XmlAttribute(AttributeName="publicAddressMatches")]
            public string PublicAddressMatches { get; set; }
            [XmlAttribute(AttributeName="presence")]
            public string Presence { get; set; }
            [XmlAttribute(AttributeName="ownerId")]
            public string OwnerId { get; set; }
            [XmlAttribute(AttributeName="home")]
            public string Home { get; set; }
            [XmlAttribute(AttributeName="sourceTitle")]
            public string SourceTitle { get; set; }
        }

        [XmlRoot(ElementName="Connection")]
        public class Connection
        {
            [XmlAttribute(AttributeName="protocol")]
            public string Protocol { get; set; }
            [XmlAttribute(AttributeName="address")]
            public string Address { get; set; }
            [XmlAttribute(AttributeName="port")]
            public string Port { get; set; }
            [XmlAttribute(AttributeName="uri")]
            public string Uri { get; set; }
            [XmlAttribute(AttributeName="local")]
            public string Local { get; set; }
            [XmlAttribute(AttributeName="relay")]
            public string Relay { get; set; }
        }
    }
}

