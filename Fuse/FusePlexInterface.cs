using System.Linq;
using System.Threading.Tasks;
using Pfs.Plex;
using Pfs.Plex.Model;
using System;
namespace Pfs.Fuse
{
    public class FusePlexInterface : FileSystem
    {
        private readonly Configuration _config;

        public FusePlexInterface(Configuration config) : base(config)
        {
            _config = config;
        }

        public FuseStatusCode release(string path, long fd)
        {
            return FuseStatusCode.Success;
        }

        public FuseStatusCode releasedir(string path, long fd)
        {
            return release(path, fd);
        }

        public async Task<FuseAttributes> getattr(string path)
        {
            var file = await GetFile(Utils.NormalisePath(path));
            if (file == null)
            {
                throw new FuseException(FuseStatusCode.ENOENT);
            }
            return new FuseAttributes(file, _config);
        }

        private async Task<long> _BaseOpen(string path, long flags, FileType type) 
        {
            path = Utils.NormalisePath(path);
            if ((flags & 3) != 0)
            {
                // this fs is read only, reject
                throw new FuseException(FuseStatusCode.EACCES);
            }
            var file = await GetFile(path);
            if (file == null)
            {
                throw new FuseException(FuseStatusCode.ENOENT);
            }
            switch (type)
            {
                case FileType.Folder when file.Type == FileType.File:
                    throw new FuseException(FuseStatusCode.ENOTDIR);
                case FileType.File when file.Type == FileType.Folder:
                    throw new FuseException(FuseStatusCode.EISDIR);
                default:
                    return (long) FuseStatusCode.Success;
            }
        }

        public async Task<long> open(string path, long flags)
        {
            return await _BaseOpen(path, flags, FileType.File);
        }

        public async Task<long> opendir(string path, long flags)
        {
            return await _BaseOpen(path, flags, FileType.Folder);
        }

        public async Task<string[]> readdir(string path)
        {
            var files = await ListFiles(Utils.NormalisePath(path));
            return files.Select(f => f.Name).ToArray();
        }

        public async Task<long> read(string path, byte[] buffer, long position)
        {
            try
            {
                var p = Utils.NormalisePath(path);
                return await this.OpenFile(p, position, buffer);
            }
            catch (InvalidOperationException e)
            {
                if (e.Message == "No such file")
                {
                    throw new FuseException(FuseStatusCode.ENOENT);
                }
                throw e;
            }
        }
    }
}
