using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Pfs.Plex.Model;

namespace Pfs.Plex.Api
{
    public class ServersClient
    {
        private readonly BaseApiClient _client;
        private readonly Random _random;

        public ServersClient(BaseApiClient client)
        {
            _client = client;
            _random = new Random();
        }

        private async Task<string> _FindServer(IEnumerable<string> uris)
        {
            var tasks = uris.Select(uri => _client.HeadFetch(uri)).ToList();
            return await Utils.FirstSuccessfulTask<string>(tasks);
        }

        private async Task<ServerNode> ToServer(Device device)
        {
            var url = await _FindServer(device.Connection.Select(c => c.Uri));
            return url == null ? null : new ServerNode(
                _random.Next(),
                device.Name,
                device.CreatedAt.ToDateTime(),
                device.LastSeenAt.ToDateTime(),
                device.AccessToken,
                url
            );
        }

        public async Task<IEnumerable<ServerNode>> ListServers()
        {
            var servers = await _client.XmlFetch<MediaContainer>("https://plex.tv", "/api/resources", new Dictionary<string, string>
            {
                { "includeHttps", "1" },
                { "includeRelay", "1" }
            });

            var serverCount = servers.Device?.Count ?? 0;
            if (serverCount == 0)
            {
                Console.WriteLine("You do not have access to any plex servers!");
                return new List<ServerNode>();
            }

            if ((servers.Device == null || int.Parse(servers.Size) != servers.Device.Count) && Environment.GetEnvironmentVariable("DEBUG") != null)
            {
                Console.WriteLine($"User should have access to {servers.Size} servers, but none were returned.");
            }

            var filtered = (await Task.WhenAll((servers.Device ?? throw new InvalidOperationException())
                .Where(d => d.Presence == "1")
                .Select(ToServer)))
                .Where(d => !string.IsNullOrWhiteSpace(d?.Url))
                .ToList();
            
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
            [XmlAttribute(AttributeName="createdAt")]
            public string CreatedAt { get; set; }
            [XmlAttribute(AttributeName="lastSeenAt")]
            public string LastSeenAt { get; set; }
            [XmlAttribute(AttributeName="accessToken")]
            public string AccessToken { get; set; }
            [XmlAttribute(AttributeName="presence")]
            public string Presence { get; set; }
        }

        [XmlRoot(ElementName="Connection")]
        public class Connection
        {
            [XmlAttribute(AttributeName="uri")]
            public string Uri { get; set; }
        }
    }
}

