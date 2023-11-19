using System;

namespace Pfs.Plex.Model
{
    public abstract class Node
    {
        public string Id { get; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; }
        public DateTime LastModified { get; }
        public FileType Type { get; }

        protected Node(string id, string name, DateTime createdAt, DateTime lastModified, FileType type)
        {
            Id = id;
            Name = name;
            CreatedAt = createdAt;
            LastModified = lastModified;
            Type = type;
        }
    }
}
