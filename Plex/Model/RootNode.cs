using System;

namespace Pfs.Plex.Model
{
    public class RootNode : Node
    {
        public RootNode() : base(0, "Plex", DateTime.Now, DateTime.Now, FileType.Folder)
        {
        }
    }
}
