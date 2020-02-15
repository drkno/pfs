using System;

namespace Pfs.Plex.Model
{
    public class FileSystemNode : Node
    {
        public ServerNode Server { get; }
        public string Next { get; }
        public long Size { get; }

        public FileSystemNode(long id, string name, DateTime createdAt, DateTime lastModified, FileType fileType, ServerNode server, string next, long size = 4096)
            : base(id, name, createdAt, lastModified, fileType)
        {
            Server = server;
            Next = next;
            Size = size;
        }
    }
}
