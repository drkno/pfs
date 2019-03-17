using System;

namespace Pfs.Plex
{
    public class BaseNode
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public FileType Type { get; set; }
    }
}
