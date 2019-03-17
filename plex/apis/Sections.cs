using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Pfs.Plex.Apis
{
    public class SectionsClient
    {
        private BaseClient _client;

        public SectionsClient(BaseClient client)
        {
            this._client = client;
        }

        public async Task<IEnumerable<Node>> ListSections(Server server)
        {
            var sections = await this._client.JsonFetch<RootObject>(server.Url, "/library/sections", new Dictionary<string, string>()
            {
                { "X-Plex-Token", server.Token }
            });

            if (sections.MediaContainer.Directory == null) 
            {
                if (Environment.GetEnvironmentVariable("DEBUG") != null && sections.MediaContainer.size != 0)
                {
                    Console.WriteLine($"User should have access to {sections.MediaContainer.size} sections, but none were returned.");
                }
                return new List<Node>();
            }

            var results = sections.MediaContainer.Directory.Select(d => new Node()
            {
                Server = server,
                Name = d.title,
                CreatedAt = DateTime.FromFileTime(d.createdAt * 1000),
                LastModified = DateTime.FromFileTime(d.updatedAt * 1000),
                Id = long.Parse(d.key),
                Type = FileType.Folder,
                Next = $"/library/sections/{d.key}/all"
            });
            Utils.CleanAndDedupe(results);
            return results;
        }

        public async Task<IEnumerable<Node>> ListSectionItems(Node section)
        {
            if (section.Server == null || section.Server.Url == null || section.Next == null) {
                throw new Exception("No such file or folder");
            }

            var sectionItems = await this._client.JsonFetch<RootObject2>(section.Server.Url, section.Next, new Dictionary<string, string>() {
                { "X-Plex-Token", section.Server.Token }
            });

            if (sectionItems.MediaContainer?.Metadata == null) {
                if (Environment.GetEnvironmentVariable("DEBUG") != null && sectionItems.MediaContainer.size != 0) {
                    Console.WriteLine($"No items were returned from plex when there should have been {sectionItems.MediaContainer.size} items.");
                }
                return new List<Node>();
            }

            var res = sectionItems.MediaContainer.Metadata.SelectMany(d => {
                if (d.Media != null) {
                    return d.Media.SelectMany(m => m.Part).Select(p => new Node() {
                        Server = section.Server,
                        Name = p.file,
                        CreatedAt = DateTime.FromFileTime(d.addedAt * 1000),
                        LastModified = DateTime.FromFileTime(d.updatedAt * 1000),
                        Id = p.id,
                        Next = p.key,
                        Type = FileType.File,
                        Size = p.size
                    });
                }
                else {
                    return new Node[] {
                        new Node() {
                            Server = section.Server,
                            Name = d.title,
                            CreatedAt = DateTime.FromFileTime(d.addedAt * 1000),
                            LastModified = DateTime.FromFileTime(d.updatedAt * 1000),
                            Id = long.Parse(d.ratingKey),
                            Next = d.key,
                            Type = FileType.Folder
                        }
                    };
                }
            });
            
            Utils.CleanAndDedupe(res);
            return res;
        }

        public class RootObject
        {
            public MediaContainer1 MediaContainer { get; set; }
        }

        public class RootObject2
        {
            public MediaContainer2 MediaContainer { get; set; }
        }

        public class MediaContainer1
        {
            public int size { get; set; }
            public bool allowSync { get; set; }
            public string identifier { get; set; }
            public string mediaTagPrefix { get; set; }
            public int mediaTagVersion { get; set; }
            public string title1 { get; set; }
            public List<Directory> Directory { get; set; }
        }

        public class MediaContainer2
        {
            public int size { get; set; }
            public int totalSize { get; set; }
            public bool allowSync { get; set; }
            public string art { get; set; }
            public string banner { get; set; }
            public string grandparentContentRating { get; set; }
            public int grandparentRatingKey { get; set; }
            public string grandparentStudio { get; set; }
            public string grandparentTheme { get; set; }
            public string grandparentThumb { get; set; }
            public string grandparentTitle { get; set; }
            public string identifier { get; set; }
            public string key { get; set; }
            public int librarySectionID { get; set; }
            public string librarySectionTitle { get; set; }
            public string librarySectionUUID { get; set; }
            public string mediaTagPrefix { get; set; }
            public int mediaTagVersion { get; set; }
            public bool nocache { get; set; }
            public int offset { get; set; }
            public int parentIndex { get; set; }
            public string parentTitle { get; set; }
            public string theme { get; set; }
            public string thumb { get; set; }
            public string title1 { get; set; }
            public string title2 { get; set; }
            public string viewGroup { get; set; }
            public int viewMode { get; set; }
            public List<Metadata> Metadata { get; set; }
        }

        public class Directory
        {
            public bool allowSync { get; set; }
            public string art { get; set; }
            public string composite { get; set; }
            public bool filters { get; set; }
            public bool refreshing { get; set; }
            public string thumb { get; set; }
            public string key { get; set; }
            public string type { get; set; }
            public string title { get; set; }
            public string agent { get; set; }
            public string scanner { get; set; }
            public string language { get; set; }
            public string uuid { get; set; }
            public int updatedAt { get; set; }
            public int createdAt { get; set; }
            public int scannedAt { get; set; }
            public List<Location> Location { get; set; }
        }

        public class Location
        {
            public int id { get; set; }
            public string path { get; set; }
        }

        public class Medium
        {
            public int id { get; set; }
            public int duration { get; set; }
            public int bitrate { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public double aspectRatio { get; set; }
            public int audioChannels { get; set; }
            public string audioCodec { get; set; }
            public string videoCodec { get; set; }
            public string videoResolution { get; set; }
            public string container { get; set; }
            public string videoFrameRate { get; set; }
            public string audioProfile { get; set; }
            public string videoProfile { get; set; }
            public List<Part> Part { get; set; }
        }

        public class Part
        {
            public int id { get; set; }
            public string key { get; set; }
            public int duration { get; set; }
            public string file { get; set; }
            public long size { get; set; }
            public string audioProfile { get; set; }
            public string container { get; set; }
            public string videoProfile { get; set; }
        }

        public class Director
        {
            public string tag { get; set; }
        }

        public class Writer
        {
            public string tag { get; set; }
        }

        public class Metadata
        {
            public string ratingKey { get; set; }
            public string key { get; set; }
            public string parentRatingKey { get; set; }
            public string grandparentRatingKey { get; set; }
            public string type { get; set; }
            public string title { get; set; }
            public string grandparentKey { get; set; }
            public string parentKey { get; set; }
            public string grandparentTitle { get; set; }
            public string parentTitle { get; set; }
            public string contentRating { get; set; }
            public string summary { get; set; }
            public int index { get; set; }
            public int parentIndex { get; set; }
            public int year { get; set; }
            public string thumb { get; set; }
            public string art { get; set; }
            public string parentThumb { get; set; }
            public string grandparentThumb { get; set; }
            public string grandparentArt { get; set; }
            public string grandparentTheme { get; set; }
            public int duration { get; set; }
            public string originallyAvailableAt { get; set; }
            public int addedAt { get; set; }
            public int updatedAt { get; set; }
            public List<Medium> Media { get; set; }
            public List<Director> Director { get; set; }
            public List<Writer> Writer { get; set; }
            public string titleSort { get; set; }
            public string chapterSource { get; set; }
        }
    }
}

