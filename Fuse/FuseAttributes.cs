using System;
using Pfs;
using Pfs.Plex.Model;

namespace Pfs.Fuse
{
    public class FuseAttributes
    {
        public const long DIRECTORY_MODE = 16676;
        public const long FILE_MODE = 33133;
        public long mtime { get; }
        public long atime { get; }
        public long ctime { get; }
        public short nlink { get; }
        public long size { get; }
        public long mode { get; }
        public long uid { get; }
        public long gid { get; }

        public FuseAttributes(Node node, Configuration config)
        {
            mtime = node.LastModified.ToUnixTimestamp();
            atime = node.CreatedAt.ToUnixTimestamp();
            ctime = node.LastModified.ToUnixTimestamp();
            nlink = 1;
            size = (node as FileSystemNode)?.Size ?? 4096;
            mode = node.Type == FileType.Folder ? DIRECTORY_MODE : FILE_MODE;
            uid = config.Uid;
            gid = config.Gid;
        }
    }
}