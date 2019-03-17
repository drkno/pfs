using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Pfs.Plex;

namespace Pfs.Fuse
{
    public class FuseAttributes
    {
        private const long DIRECTORY_MODE = 16676;
        private const long FILE_MODE = 33133;
        public long mtime { get; }
        public long atime { get; }
        public long ctime { get; }
        public short nlink { get; }
        public long size { get; }
        public long mode { get; }
        public long uid { get; }
        public long gid { get; }

        private static long ToUnixTimestamp(DateTime dateTime)
        {
            return (long) (TimeZoneInfo.ConvertTimeToUtc(dateTime) - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }

        public FuseAttributes(BaseNode node, Configuration config) {
            mtime = ToUnixTimestamp(node.LastModified);
            atime = ToUnixTimestamp(node.CreatedAt);
            ctime = ToUnixTimestamp(node.LastModified);
            nlink = 1;
            size = (node as Node)?.Size ?? 0;
            mode = node.Type == FileType.Folder ? DIRECTORY_MODE : FILE_MODE;
            uid = config.Uid;
            gid = config.Gid;
        }
    }
}