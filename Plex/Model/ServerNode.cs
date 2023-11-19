using System;

namespace Pfs.Plex.Model
{
    public class ServerNode : Node
    {
        public string Token { get; }
        public string Url { get; }

        public ServerNode(string id, string name, DateTime createdAt, DateTime lastModified, string token, string url)
            : base(id, name, createdAt, lastModified, FileType.Folder)
        {
            Token = token;
            Url = url;
        }
    }
}
