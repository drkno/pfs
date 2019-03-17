using System;
using System.Threading.Tasks;

namespace Pfs.Plex
{
    public class Node : BaseNode
    {
        public Server Server { get; set; }
        public string Next { get; set; }
        public long Size { get; set; }
    }
}
