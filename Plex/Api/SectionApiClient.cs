using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pfs.Plex.Model;

namespace Pfs.Plex.Api
{
    public class SectionsClient
    {
        private readonly BaseApiClient _client;

        public SectionsClient(BaseApiClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<FileSystemNode>> ListSections(ServerNode server)
        {
            var sections = await _client.JsonFetch<SectionJsonModel>(server.Url, "/library/sections", new Dictionary<string, string>
            {
                { "X-Plex-Token", server.Token }
            });

            if (sections?.MediaContainer?.DirectoryItems == null) 
            {
                if (Environment.GetEnvironmentVariable("DEBUG") != null && sections?.MediaContainer?.Size != 0)
                {
                    Console.WriteLine($"User should have access to {sections?.MediaContainer?.Size} sections, but none were returned.");
                }
                return new List<FileSystemNode>();
            }

            var results = sections.MediaContainer.DirectoryItems.Select(d => new FileSystemNode(
                d.RatingKey,
                d.Title,
                d.AddedAt,
                d.UpdatedAt,
                FileType.Folder,
                server,
                $"/library/sections/{d.Key}/all"
            )).ToList();
            Utils.CleanAndDedupe(results);
            return results;
        }

        public async Task<IEnumerable<FileSystemNode>> ListSectionItems(FileSystemNode section)
        {
            if (section.Server?.Url == null || section.Next == null)
            {
                throw new Exception("No such file or folder");
            }

            var sectionItems = await _client.JsonFetch<SectionJsonModel>(section.Server.Url, section.Next, new Dictionary<string, string>
            {
                { "X-Plex-Token", section.Server.Token }
            });

            if (sectionItems.MediaContainer?.MetadataItems == null)
            {
                if (Environment.GetEnvironmentVariable("DEBUG") != null && sectionItems.MediaContainer?.Size != 0)
                {
                    Console.WriteLine($"No items were returned from plex when there should have been {sectionItems.MediaContainer?.Size} items.");
                }
                return new List<FileSystemNode>();
            }

            var res = sectionItems.MediaContainer.MetadataItems.SelectMany(d =>
            {
                if (d.Media != null)
                {
                    return d.Media.SelectMany(m => m.Parts).Select(p => new FileSystemNode(
                        p.Id,
                        Path.GetFileName(Utils.NormalisePath(p.File)),
                        d.AddedAt,
                        d.UpdatedAt,
                        FileType.File,
                        section.Server,
                        p.Key,
                        p.Size
                    ));
                }
                else
                {
                    return new[] {
                        new FileSystemNode(
                            d.RatingKey,
                            d.Title,
                            d.AddedAt,
                            d.UpdatedAt,
                            FileType.Folder,
                            section.Server,
                            d.Key
                        )
                    };
                }
            }).ToList();
            
            Utils.CleanAndDedupe(res);
            return res;
        }

        private class SectionJsonModel
        {
            public MediaContainer MediaContainer { get; set; }
        }

        private class MediaContainer
        {
            [JsonProperty("size")]
            public int Size { get; set; }
            [JsonProperty("Metadata")]
            public List<Metadata> MetadataItems { get; set; }
            [JsonProperty("Directory")]
            public List<Metadata> DirectoryItems { get; set; }
        }

        private class Metadata
        {
            [JsonProperty("ratingKey")]
            public long RatingKey { get; set; }
            [JsonProperty("key")]
            public string Key { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("addedAt")]
            [JsonConverter(typeof(Utils.PlexDateTimeConverter))]
            public DateTime AddedAt { get; set; }
            [JsonProperty("createdAt")]
            [JsonConverter(typeof(Utils.PlexDateTimeConverter))]
            private DateTime CreatedAt
            {
                set => AddedAt = value;
            }
            [JsonProperty("updatedAt")]
            [JsonConverter(typeof(Utils.PlexDateTimeConverter))]
            public DateTime UpdatedAt { get; set; }
            public List<Medium> Media { get; set; }
        }

        private class Medium
        {
            [JsonProperty("Part")]
            public List<Part> Parts { get; set; }
        }

        private class Part
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("key")]
            public string Key { get; set; }
            [JsonProperty("file")]
            public string File { get; set; }
            [JsonProperty("size")]
            public long Size { get; set; }
        }
    }
}

